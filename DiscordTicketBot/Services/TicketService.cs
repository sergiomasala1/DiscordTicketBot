using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DiscordTicketBot.Database;
using DiscordTicketBot.Models;

namespace DiscordTicketBot.Services;

/// <summary>
/// Serviço principal para gerenciamento de chamados e controle de tempo.
/// Centraliza toda a lógica de negócio.
/// </summary>
public class TicketService
{
    private readonly AppDbContext _db;
    private readonly ILogger<TicketService> _logger;

    public TicketService(AppDbContext db, ILogger<TicketService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ──────────────────────────────────────────────────────────────
    // Gerenciamento de Usuários
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Obtém ou cria um usuário no banco com base no ID Discord.
    /// </summary>
    public async Task<User> GetOrCreateUserAsync(ulong discordUserId, string username, string? displayName = null)
    {
        var userId = discordUserId.ToString();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.DiscordUserId == userId);

        if (user is null)
        {
            user = new User
            {
                DiscordUserId = userId,
                Username = username,
                DisplayName = displayName ?? username
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Novo usuário registrado: {Username} ({Id})", username, userId);
        }
        else
        {
            // Atualiza nome se necessário
            if (user.Username != username || user.DisplayName != (displayName ?? username))
            {
                user.Username = username;
                user.DisplayName = displayName ?? username;
                user.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        return user;
    }

    // ──────────────────────────────────────────────────────────────
    // Criação e Controle de Chamados
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Inicia um novo chamado para o usuário. Retorna erro se já houver um ativo.
    /// </summary>
    public async Task<(Ticket? ticket, string? error)> StartTicketAsync(
        ulong discordUserId,
        string username,
        string? displayName,
        string ticketNumber,
        string? description)
    {
        var user = await GetOrCreateUserAsync(discordUserId, username, displayName);

        // Verifica se há chamado ativo
        var active = await GetActiveTicketAsync(discordUserId);
        if (active is not null)
        {
            return (null, $"Você já possui o chamado **#{active.TicketNumber}** ativo. Finalize-o antes de iniciar um novo.");
        }

        // Verifica se há chamado pausado
        var paused = await _db.Tickets
            .Include(t => t.PauseSessions)
            .Where(t => t.UserId == user.Id && t.Status == TicketStatus.Paused)
            .FirstOrDefaultAsync();

        if (paused is not null)
        {
            return (null, $"Você possui o chamado **#{paused.TicketNumber}** pausado. Retome-o ou finalize-o antes de iniciar um novo.");
        }

        var ticket = new Ticket
        {
            TicketNumber = ticketNumber.Trim(),
            Description = description?.Trim(),
            Status = TicketStatus.Active,
            StartedAt = DateTime.UtcNow,
            UserId = user.Id
        };

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        // Cria primeira sessão de tempo
        var session = new TimeSession
        {
            TicketId = ticket.Id,
            StartedAt = ticket.StartedAt
        };
        _db.TimeSessions.Add(session);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Chamado #{Number} iniciado por {User}", ticketNumber, username);
        return (ticket, null);
    }

    /// <summary>
    /// Pausa o chamado ativo do usuário.
    /// </summary>
    public async Task<(Ticket? ticket, string? error)> PauseTicketAsync(ulong discordUserId, int ticketId)
    {
        var ticket = await GetTicketWithDetailsAsync(ticketId);

        if (ticket is null)
            return (null, "Chamado não encontrado.");

        if (ticket.User.DiscordUserId != discordUserId.ToString())
            return (null, "Você não tem permissão para pausar este chamado.");

        if (ticket.Status != TicketStatus.Active)
            return (null, "Este chamado não está ativo.");

        // Encerra a sessão de tempo atual
        var session = ticket.TimeSessions.FirstOrDefault(s => s.EndedAt == null);
        if (session is not null)
        {
            session.EndedAt = DateTime.UtcNow;
        }

        // Cria uma pausa
        var pause = new PauseSession
        {
            TicketId = ticket.Id,
            PausedAt = DateTime.UtcNow
        };
        _db.PauseSessions.Add(pause);

        ticket.Status = TicketStatus.Paused;
        ticket.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Chamado #{Number} pausado", ticket.TicketNumber);
        return (ticket, null);
    }

    /// <summary>
    /// Retoma um chamado pausado.
    /// </summary>
    public async Task<(Ticket? ticket, string? error)> ResumeTicketAsync(ulong discordUserId, int ticketId)
    {
        var ticket = await GetTicketWithDetailsAsync(ticketId);

        if (ticket is null)
            return (null, "Chamado não encontrado.");

        if (ticket.User.DiscordUserId != discordUserId.ToString())
            return (null, "Você não tem permissão para retomar este chamado.");

        if (ticket.Status != TicketStatus.Paused)
            return (null, "Este chamado não está pausado.");

        // Encerra a pausa atual e acumula o tempo
        var pause = ticket.PauseSessions.FirstOrDefault(p => p.ResumedAt == null);
        if (pause is not null)
        {
            pause.ResumedAt = DateTime.UtcNow;
            ticket.TotalPausedSeconds += (long)(pause.ResumedAt.Value - pause.PausedAt).TotalSeconds;
        }

        // Cria nova sessão de tempo
        var session = new TimeSession
        {
            TicketId = ticket.Id,
            StartedAt = DateTime.UtcNow
        };
        _db.TimeSessions.Add(session);

        ticket.Status = TicketStatus.Active;
        ticket.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Chamado #{Number} retomado", ticket.TicketNumber);
        return (ticket, null);
    }

    /// <summary>
    /// Finaliza um chamado (ativo ou pausado).
    /// </summary>
    public async Task<(Ticket? ticket, string? error)> FinishTicketAsync(ulong discordUserId, int ticketId)
    {
        var ticket = await GetTicketWithDetailsAsync(ticketId);

        if (ticket is null)
            return (null, "Chamado não encontrado.");

        if (ticket.User.DiscordUserId != discordUserId.ToString())
            return (null, "Você não tem permissão para finalizar este chamado.");

        if (ticket.Status == TicketStatus.Finished)
            return (null, "Este chamado já foi finalizado.");

        var now = DateTime.UtcNow;

        // Encerra sessão de tempo ativa (se existir)
        var session = ticket.TimeSessions.FirstOrDefault(s => s.EndedAt == null);
        if (session is not null)
            session.EndedAt = now;

        // Encerra pausa ativa (se existir) e acumula
        var pause = ticket.PauseSessions.FirstOrDefault(p => p.ResumedAt == null);
        if (pause is not null)
        {
            pause.ResumedAt = now;
            ticket.TotalPausedSeconds += (long)(now - pause.PausedAt).TotalSeconds;
        }

        ticket.Status = TicketStatus.Finished;
        ticket.FinishedAt = now;
        ticket.UpdatedAt = now;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Chamado #{Number} finalizado. Tempo líquido: {Net}",
            ticket.TicketNumber, ticket.NetElapsedFormatted);

        return (ticket, null);
    }

    /// <summary>
    /// Salva os IDs do embed Discord no chamado para futuras atualizações.
    /// </summary>
    public async Task SaveMessageReferenceAsync(int ticketId, ulong messageId, ulong channelId)
    {
        var ticket = await _db.Tickets.FindAsync(ticketId);
        if (ticket is null) return;

        ticket.DiscordMessageId = messageId.ToString();
        ticket.DiscordChannelId = channelId.ToString();
        ticket.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────────────────────
    // Consultas
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Retorna o chamado ativo do usuário (se existir).
    /// </summary>
    public async Task<Ticket?> GetActiveTicketAsync(ulong discordUserId)
    {
        var userId = discordUserId.ToString();
        return await _db.Tickets
            .Include(t => t.User)
            .Include(t => t.PauseSessions)
            .Include(t => t.TimeSessions)
            .Where(t => t.User.DiscordUserId == userId && t.Status == TicketStatus.Active)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Retorna todos os chamados pausados do usuário.
    /// </summary>
    public async Task<List<Ticket>> GetPausedTicketsAsync(ulong discordUserId)
    {
        var userId = discordUserId.ToString();
        return await _db.Tickets
            .Include(t => t.User)
            .Include(t => t.PauseSessions)
            .Include(t => t.TimeSessions)
            .Where(t => t.User.DiscordUserId == userId && t.Status == TicketStatus.Paused)
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Retorna um chamado pelo ID com todos os detalhes.
    /// </summary>
    public async Task<Ticket?> GetTicketWithDetailsAsync(int ticketId)
    {
        return await _db.Tickets
            .Include(t => t.User)
            .Include(t => t.PauseSessions)
            .Include(t => t.TimeSessions)
            .FirstOrDefaultAsync(t => t.Id == ticketId);
    }

    /// <summary>
    /// Retorna histórico recente de chamados finalizados do usuário.
    /// </summary>
    public async Task<List<Ticket>> GetRecentFinishedTicketsAsync(ulong discordUserId, int take = 5)
    {
        var userId = discordUserId.ToString();
        return await _db.Tickets
            .Include(t => t.User)
            .Include(t => t.PauseSessions)
            .Include(t => t.TimeSessions)
            .Where(t => t.User.DiscordUserId == userId && t.Status == TicketStatus.Finished)
            .OrderByDescending(t => t.FinishedAt)
            .Take(take)
            .ToListAsync();
    }
}
