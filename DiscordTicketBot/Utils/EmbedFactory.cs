using Discord;
using DiscordTicketBot.Models;

namespace DiscordTicketBot.Utils;

/// <summary>
/// Fábrica de embeds Discord com visual moderno e profissional.
/// Utiliza cores e ícones consistentes para cada estado do chamado.
/// </summary>
public static class EmbedFactory
{
    // ── Paleta de cores por status ──────────────────────────────
    private static readonly Color ColorActive  = new(0x2ECC71);   // Verde
    private static readonly Color ColorPaused  = new(0xF39C12);   // Amarelo/Laranja
    private static readonly Color ColorFinished = new(0x95A5A6);  // Cinza
    private static readonly Color ColorError   = new(0xE74C3C);   // Vermelho
    private static readonly Color ColorInfo    = new(0x3498DB);   // Azul

    // ── Ícones por status ───────────────────────────────────────
    private static string StatusIcon(TicketStatus status) => status switch
    {
        TicketStatus.Active   => "🟢",
        TicketStatus.Paused   => "🟡",
        TicketStatus.Finished => "⚪",
        _ => "❓"
    };

    private static string StatusLabel(TicketStatus status) => status switch
    {
        TicketStatus.Active   => "Ativo",
        TicketStatus.Paused   => "Pausado",
        TicketStatus.Finished => "Finalizado",
        _ => "Desconhecido"
    };

    private static Color StatusColor(TicketStatus status) => status switch
    {
        TicketStatus.Active   => ColorActive,
        TicketStatus.Paused   => ColorPaused,
        TicketStatus.Finished => ColorFinished,
        _ => ColorInfo
    };

    // ──────────────────────────────────────────────────────────────
    // Embed principal do chamado
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Cria o embed principal exibido ao iniciar, pausar, retomar ou atualizar um chamado.
    /// </summary>
    public static Embed BuildTicketEmbed(Ticket ticket)
    {
        var technician = ticket.User.DisplayName ?? ticket.User.Username;
        var status = ticket.Status;

        var builder = new EmbedBuilder()
            .WithTitle($"🎫  Chamado  #{ticket.TicketNumber}")
            .WithColor(StatusColor(status))
            .WithTimestamp(DateTimeOffset.UtcNow);

        // Descrição do chamado
        if (!string.IsNullOrWhiteSpace(ticket.Description))
            builder.WithDescription($"> {ticket.Description}");

        // Campos principais
        builder.AddField("👤  Técnico", $"`{technician}`", inline: true);
        builder.AddField($"{StatusIcon(status)}  Status", $"`{StatusLabel(status)}`", inline: true);
        builder.AddField("\u200b", "\u200b", inline: true); // Espaçador

        builder.AddField("⏱️  Tempo Líquido", $"```{ticket.NetElapsedFormatted}```", inline: true);
        builder.AddField("🕐  Tempo Total", $"```{ticket.TotalElapsedFormatted}```", inline: true);

        // Tempo pausado (se houver)
        if (ticket.TotalPausedSeconds > 0 || status == TicketStatus.Paused)
        {
            var pauseTs = TimeSpan.FromSeconds(ticket.TotalPausedSeconds);
            var activePause = ticket.PauseSessions.FirstOrDefault(p => p.ResumedAt == null);
            if (activePause is not null)
                pauseTs += DateTime.UtcNow - activePause.PausedAt;

            builder.AddField("⏸️  Tempo Pausado",
                $"```{FormatTs(pauseTs)}```", inline: true);
        }

        // Linha do tempo
        builder.AddField("📅  Iniciado em",
            $"<t:{new DateTimeOffset(ticket.StartedAt).ToUnixTimeSeconds()}:R>", inline: true);

        if (ticket.FinishedAt.HasValue)
            builder.AddField("🏁  Finalizado em",
                $"<t:{new DateTimeOffset(ticket.FinishedAt.Value).ToUnixTimeSeconds()}:R>", inline: true);

        // Número de pausas
        var pauseCount = ticket.PauseSessions.Count;
        if (pauseCount > 0)
            builder.AddField("🔄  Pausas", $"`{pauseCount}x`", inline: true);

        builder.WithFooter($"ID Interno: {ticket.Id}  •  DiscordTicketBot");

        return builder.Build();
    }

    // ──────────────────────────────────────────────────────────────
    // Embed de status (comando /status)
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Cria o embed exibido pelo comando /status.
    /// </summary>
    public static Embed BuildStatusEmbed(
        Ticket? activeTicket,
        List<Ticket> pausedTickets,
        string userName)
    {
        var builder = new EmbedBuilder()
            .WithTitle($"📊  Status de Chamados  —  {userName}")
            .WithColor(ColorInfo)
            .WithTimestamp(DateTimeOffset.UtcNow);

        // ── Chamado ativo ──
        if (activeTicket is not null)
        {
            builder.AddField(
                "🟢  Chamado Ativo",
                $"**#{activeTicket.TicketNumber}**\n" +
                $"⏱️ Tempo: `{activeTicket.NetElapsedFormatted}`\n" +
                $"🕐 Iniciado: <t:{new DateTimeOffset(activeTicket.StartedAt).ToUnixTimeSeconds()}:R>",
                inline: false);
        }
        else
        {
            builder.AddField("🟢  Chamado Ativo", "*Nenhum chamado ativo no momento.*", inline: false);
        }

        // ── Chamados pausados ──
        if (pausedTickets.Count > 0)
        {
            var pausedText = string.Join("\n", pausedTickets.Select(t =>
                $"🟡 **#{t.TicketNumber}** — ⏱️ `{t.NetElapsedFormatted}` " +
                $"| pausado <t:{new DateTimeOffset(t.UpdatedAt).ToUnixTimeSeconds()}:R>"));

            builder.AddField($"⏸️  Chamados Pausados ({pausedTickets.Count})", pausedText, inline: false);
        }
        else
        {
            builder.AddField("⏸️  Chamados Pausados", "*Nenhum chamado pausado.*", inline: false);
        }

        builder.WithFooter("DiscordTicketBot  •  Controle de Tempo de Chamados");

        return builder.Build();
    }

    // ──────────────────────────────────────────────────────────────
    // Embed de erro
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Cria um embed de erro padronizado.
    /// </summary>
    public static Embed BuildErrorEmbed(string message, string? detail = null)
    {
        var builder = new EmbedBuilder()
            .WithTitle("❌  Erro")
            .WithDescription(message)
            .WithColor(ColorError)
            .WithTimestamp(DateTimeOffset.UtcNow);

        if (!string.IsNullOrWhiteSpace(detail))
            builder.AddField("Detalhes", $"```{detail}```");

        return builder.Build();
    }

    /// <summary>
    /// Cria um embed de sucesso simples.
    /// </summary>
    public static Embed BuildSuccessEmbed(string message)
    {
        return new EmbedBuilder()
            .WithDescription($"✅  {message}")
            .WithColor(ColorActive)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
    }

    // ── Helpers ────────────────────────────────────────────────
    private static string FormatTs(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes:D2}m {ts.Seconds:D2}s";
        if (ts.TotalMinutes >= 1)
            return $"{ts.Minutes}m {ts.Seconds:D2}s";
        return $"{ts.Seconds}s";
    }
}
