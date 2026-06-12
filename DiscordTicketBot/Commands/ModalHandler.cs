using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using DiscordTicketBot.Components;
using DiscordTicketBot.Services;
using DiscordTicketBot.Utils;

namespace DiscordTicketBot.Commands;

/// <summary>
/// Trata o retorno dos modais enviados pelo usuário.
/// </summary>
public class ModalHandler
{
    private readonly TicketService _ticketService;
    private readonly ILogger<ModalHandler> _logger;

    public ModalHandler(TicketService ticketService, ILogger<ModalHandler> logger)
    {
        _ticketService = ticketService;
        _logger = logger;
    }

    public async Task HandleModalSubmittedAsync(SocketModal modal)
    {
        try
        {
            switch (modal.Data.CustomId)
            {
                case SlashCommandHandler.StartModalId:
                    await HandleStartModalAsync(modal);
                    break;

                default:
                    await modal.RespondAsync(
                        embed: EmbedFactory.BuildErrorEmbed("Modal não reconhecido."),
                        ephemeral: true);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar modal {ModalId}", modal.Data.CustomId);
            try
            {
                await modal.RespondAsync(
                    embed: EmbedFactory.BuildErrorEmbed("Erro ao processar formulário.", ex.Message),
                    ephemeral: true);
            }
            catch { }
        }
    }

    // ──────────────────────────────────────────────────────────────
    // Modal de início de chamado
    // ──────────────────────────────────────────────────────────────

    private async Task HandleStartModalAsync(SocketModal modal)
    {
        // Extrai os valores preenchidos
        var ticketNumber = modal.Data.Components
            .FirstOrDefault(c => c.CustomId == SlashCommandHandler.TicketNumberFieldId)
            ?.Value ?? string.Empty;

        var description = modal.Data.Components
            .FirstOrDefault(c => c.CustomId == SlashCommandHandler.DescriptionFieldId)
            ?.Value;

        if (string.IsNullOrWhiteSpace(ticketNumber))
        {
            await modal.RespondAsync(
                embed: EmbedFactory.BuildErrorEmbed("O número do chamado é obrigatório."),
                ephemeral: true);
            return;
        }

        // Diferir a resposta pois a operação de banco pode demorar
        await modal.DeferAsync();

        var guildUser = modal.User as SocketGuildUser;
        var displayName = guildUser?.DisplayName ?? modal.User.Username;

        var (ticket, error) = await _ticketService.StartTicketAsync(
            discordUserId: modal.User.Id,
            username: modal.User.Username,
            displayName: displayName,
            ticketNumber: ticketNumber,
            description: description);

        if (error is not null)
        {
            await modal.FollowupAsync(
                embed: EmbedFactory.BuildErrorEmbed(error),
                ephemeral: true);
            return;
        }

        if (ticket is null)
        {
            await modal.FollowupAsync(
                embed: EmbedFactory.BuildErrorEmbed("Não foi possível criar o chamado."),
                ephemeral: true);
            return;
        }

        // Monta embed + botões e envia
        var embed = EmbedFactory.BuildTicketEmbed(ticket);
        var components = TicketComponents.BuildActiveComponents(ticket.Id);

        var message = await modal.FollowupAsync(embed: embed, components: components);

        // Salva referência da mensagem para atualizações futuras
        await _ticketService.SaveMessageReferenceAsync(ticket.Id, message.Id, message.Channel.Id);

        _logger.LogInformation(
            "Chamado #{Number} criado por {User} via modal",
            ticketNumber, modal.User.Username);
    }
}
