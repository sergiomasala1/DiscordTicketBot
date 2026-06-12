using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using DiscordTicketBot.Components;
using DiscordTicketBot.Services;
using DiscordTicketBot.Utils;

namespace DiscordTicketBot.Commands;

/// <summary>
/// Responsável por registrar e tratar os slash commands do bot.
/// </summary>
public class SlashCommandHandler
{
    private readonly DiscordSocketClient _client;
    private readonly TicketService _ticketService;
    private readonly ILogger<SlashCommandHandler> _logger;

    // IDs dos modais para identificar o retorno do usuário
    public const string StartModalId = "modal_start_ticket";
    public const string TicketNumberFieldId = "field_ticket_number";
    public const string DescriptionFieldId = "field_description";

    public SlashCommandHandler(
        DiscordSocketClient client,
        TicketService ticketService,
        ILogger<SlashCommandHandler> logger)
    {
        _client = client;
        _ticketService = ticketService;
        _logger = logger;
    }

    // ──────────────────────────────────────────────────────────────
    // Registro dos comandos na API do Discord
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Registra os slash commands globais. Pode demorar até 1h para propagar.
    /// Para testes, prefira registrar por guild (instantâneo).
    /// </summary>
    public async Task RegisterCommandsAsync()
    {
        var commands = new List<ApplicationCommandProperties>
        {
            new SlashCommandBuilder()
                .WithName("start")
                .WithDescription("📋 Inicia o controle de tempo para um chamado de suporte")
                .Build(),

            new SlashCommandBuilder()
                .WithName("status")
                .WithDescription("📊 Exibe o status atual dos seus chamados em andamento")
                .Build()
        };

        try
        {
            // Registra em todas as guilds conectadas para propagação instantânea em dev
            foreach (var guild in _client.Guilds)
            {
                await guild.BulkOverwriteApplicationCommandAsync(commands.ToArray());
                _logger.LogInformation("Comandos registrados na guild: {Guild}", guild.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao registrar slash commands.");
        }
    }

    // ──────────────────────────────────────────────────────────────
    // Handler principal de slash commands
    // ──────────────────────────────────────────────────────────────

    public async Task HandleSlashCommandAsync(SocketSlashCommand command)
    {
        try
        {
            switch (command.CommandName)
            {
                case "start":
                    await HandleStartCommandAsync(command);
                    break;

                case "status":
                    await HandleStatusCommandAsync(command);
                    break;

                default:
                    await command.RespondAsync(
                        embed: EmbedFactory.BuildErrorEmbed("Comando não reconhecido."),
                        ephemeral: true);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar comando /{Command}", command.CommandName);
            try
            {
                await command.RespondAsync(
                    embed: EmbedFactory.BuildErrorEmbed(
                        "Ocorreu um erro interno ao processar o comando.",
                        ex.Message),
                    ephemeral: true);
            }
            catch { /* Resposta já foi enviada */ }
        }
    }

    // ──────────────────────────────────────────────────────────────
    // /start → Abre modal para coleta de dados
    // ──────────────────────────────────────────────────────────────

    private static async Task HandleStartCommandAsync(SocketSlashCommand command)
    {
        // Abre um modal (pop-up) para o técnico preencher os dados do chamado
        var modal = new ModalBuilder()
            .WithTitle("🎫  Iniciar Controle de Chamado")
            .WithCustomId(StartModalId)
            .AddTextInput(
                label: "Número do Chamado",
                customId: TicketNumberFieldId,
                style: TextInputStyle.Short,
                placeholder: "Ex: 12345",
                required: true,
                minLength: 1,
                maxLength: 50)
            .AddTextInput(
                label: "Descrição (opcional)",
                customId: DescriptionFieldId,
                style: TextInputStyle.Paragraph,
                placeholder: "Descreva brevemente o problema ou atividade...",
                required: false,
                maxLength: 500)
            .Build();

        await command.RespondWithModalAsync(modal);
    }

    // ──────────────────────────────────────────────────────────────
    // /status → Exibe situação atual dos chamados
    // ──────────────────────────────────────────────────────────────

    private async Task HandleStatusCommandAsync(SocketSlashCommand command)
    {
        await command.DeferAsync(ephemeral: true);

        var user = command.User;
        var activeTicket = await _ticketService.GetActiveTicketAsync(user.Id);
        var pausedTickets = await _ticketService.GetPausedTicketsAsync(user.Id);

        var displayName = (user as SocketGuildUser)?.DisplayName ?? user.Username;
        var embed = EmbedFactory.BuildStatusEmbed(activeTicket, pausedTickets, displayName);

        // Se houver chamado ativo, adiciona botões inline
        if (activeTicket is not null)
        {
            var components = TicketComponents.BuildActiveComponents(activeTicket.Id);
            await command.FollowupAsync(embed: embed, components: components, ephemeral: true);
        }
        else if (pausedTickets.Count > 0)
        {
            // Mostra botões do primeiro chamado pausado
            var components = TicketComponents.BuildPausedComponents(pausedTickets[0].Id);
            await command.FollowupAsync(embed: embed, components: components, ephemeral: true);
        }
        else
        {
            await command.FollowupAsync(embed: embed, ephemeral: true);
        }
    }
}
