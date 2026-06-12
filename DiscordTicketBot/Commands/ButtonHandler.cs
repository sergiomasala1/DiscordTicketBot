using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using DiscordTicketBot.Components;
using DiscordTicketBot.Services;
using DiscordTicketBot.Utils;

namespace DiscordTicketBot.Commands;

/// <summary>
/// Trata os cliques nos botões dos embeds de chamado.
/// </summary>
public class ButtonHandler
{
    private readonly TicketService _ticketService;
    private readonly ILogger<ButtonHandler> _logger;

    public ButtonHandler(TicketService ticketService, ILogger<ButtonHandler> logger)
    {
        _ticketService = ticketService;
        _logger = logger;
    }

    public async Task HandleButtonExecutedAsync(SocketMessageComponent component)
    {
        var customId = component.Data.CustomId;

        try
        {
            // ── Pausar ──────────────────────────────────────────
            if (customId.StartsWith(TicketComponents.PausePrefix))
            {
                var ticketId = ParseTicketId(customId, TicketComponents.PausePrefix);
                await HandlePauseAsync(component, ticketId);
                return;
            }

            // ── Retomar ─────────────────────────────────────────
            if (customId.StartsWith(TicketComponents.ResumePrefix))
            {
                var ticketId = ParseTicketId(customId, TicketComponents.ResumePrefix);
                await HandleResumeAsync(component, ticketId);
                return;
            }

            // ── Finalizar ───────────────────────────────────────
            if (customId.StartsWith(TicketComponents.FinishPrefix))
            {
                var ticketId = ParseTicketId(customId, TicketComponents.FinishPrefix);
                await HandleFinishAsync(component, ticketId);
                return;
            }

            // ── Botão desabilitado (chamado já finalizado) ──────
            if (customId == "ticket_done")
            {
                await component.RespondAsync(
                    embed: EmbedFactory.BuildErrorEmbed("Este chamado já foi finalizado."),
                    ephemeral: true);
                return;
            }

            await component.RespondAsync(
                embed: EmbedFactory.BuildErrorEmbed("Ação desconhecida."),
                ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar botão {CustomId}", customId);
            try
            {
                await component.RespondAsync(
                    embed: EmbedFactory.BuildErrorEmbed("Erro ao processar ação.", ex.Message),
                    ephemeral: true);
            }
            catch { }
        }
    }

    // ──────────────────────────────────────────────────────────────
    // Handlers individuais
    // ──────────────────────────────────────────────────────────────

    private async Task HandlePauseAsync(SocketMessageComponent component, int ticketId)
    {
        await component.DeferAsync();

        var (ticket, error) = await _ticketService.PauseTicketAsync(component.User.Id, ticketId);

        if (error is not null)
        {
            await component.FollowupAsync(
                embed: EmbedFactory.BuildErrorEmbed(error),
                ephemeral: true);
            return;
        }

        // Atualiza a mensagem original com novo embed + botões de pausado
        var embed = EmbedFactory.BuildTicketEmbed(ticket!);
        var components = TicketComponents.BuildPausedComponents(ticket!.Id);

        await component.Message.ModifyAsync(msg =>
        {
            msg.Embed = embed;
            msg.Components = components;
        });

        await component.FollowupAsync(
            embed: EmbedFactory.BuildSuccessEmbed($"Chamado **#{ticket.TicketNumber}** pausado."),
            ephemeral: true);
    }

    private async Task HandleResumeAsync(SocketMessageComponent component, int ticketId)
    {
        await component.DeferAsync();

        var (ticket, error) = await _ticketService.ResumeTicketAsync(component.User.Id, ticketId);

        if (error is not null)
        {
            await component.FollowupAsync(
                embed: EmbedFactory.BuildErrorEmbed(error),
                ephemeral: true);
            return;
        }

        var embed = EmbedFactory.BuildTicketEmbed(ticket!);
        var components = TicketComponents.BuildActiveComponents(ticket!.Id);

        await component.Message.ModifyAsync(msg =>
        {
            msg.Embed = embed;
            msg.Components = components;
        });

        await component.FollowupAsync(
            embed: EmbedFactory.BuildSuccessEmbed($"Chamado **#{ticket.TicketNumber}** retomado! ▶️"),
            ephemeral: true);
    }

    private async Task HandleFinishAsync(SocketMessageComponent component, int ticketId)
    {
        await component.DeferAsync();

        var (ticket, error) = await _ticketService.FinishTicketAsync(component.User.Id, ticketId);

        if (error is not null)
        {
            await component.FollowupAsync(
                embed: EmbedFactory.BuildErrorEmbed(error),
                ephemeral: true);
            return;
        }

        var embed = EmbedFactory.BuildTicketEmbed(ticket!);
        var components = TicketComponents.BuildFinishedComponents();

        await component.Message.ModifyAsync(msg =>
        {
            msg.Embed = embed;
            msg.Components = components;
        });

        await component.FollowupAsync(
            embed: EmbedFactory.BuildSuccessEmbed(
                $"Chamado **#{ticket!.TicketNumber}** finalizado! ✅\n" +
                $"⏱️ Tempo líquido trabalhado: **{ticket.NetElapsedFormatted}**"),
            ephemeral: true);
    }

    // ── Helper ─────────────────────────────────────────────────
    private static int ParseTicketId(string customId, string prefix)
    {
        var raw = customId[prefix.Length..];
        if (!int.TryParse(raw, out var id))
            throw new FormatException($"ID de chamado inválido: {raw}");
        return id;
    }
}
