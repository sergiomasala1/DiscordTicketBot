using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DiscordTicketBot.Commands;
using DiscordTicketBot.Database;

namespace DiscordTicketBot.Services;

/// <summary>
/// Serviço hospedado que gerencia o ciclo de vida do bot Discord.
/// Inicializa a conexão, registra eventos e mantém o bot online.
/// </summary>
public class BotService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly IConfiguration _config;
    private readonly ILogger<BotService> _logger;
    private readonly DatabaseInitializer _dbInit;
    private readonly SlashCommandHandler _slashHandler;
    private readonly ModalHandler _modalHandler;
    private readonly ButtonHandler _buttonHandler;

    public BotService(
        DiscordSocketClient client,
        IConfiguration config,
        ILogger<BotService> logger,
        DatabaseInitializer dbInit,
        SlashCommandHandler slashHandler,
        ModalHandler modalHandler,
        ButtonHandler buttonHandler)
    {
        _client = client;
        _config = config;
        _logger = logger;
        _dbInit = dbInit;
        _slashHandler = slashHandler;
        _modalHandler = modalHandler;
        _buttonHandler = buttonHandler;
    }

    // ──────────────────────────────────────────────────────────────
    // IHostedService
    // ──────────────────────────────────────────────────────────────

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // 1. Inicializa banco de dados
        await _dbInit.InitializeAsync();

        // 2. Registra eventos do Discord
        RegisterEvents();

        // 3. Autentica e conecta o bot
        var token = _config["Discord:Token"]
            ?? Environment.GetEnvironmentVariable("DISCORD_TOKEN")
            ?? throw new InvalidOperationException(
                "Token do Discord não encontrado. Configure Discord:Token no appsettings.json " +
                "ou a variável de ambiente DISCORD_TOKEN.");

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        _logger.LogInformation("Bot Discord iniciando...");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Bot Discord encerrando...");
        await _client.StopAsync();
        await _client.LogoutAsync();
    }

    // ──────────────────────────────────────────────────────────────
    // Registro de eventos
    // ──────────────────────────────────────────────────────────────

    private void RegisterEvents()
    {
        // Log interno do Discord.Net → nosso logger
        _client.Log += OnLogAsync;

        // Bot ficou pronto (conectado e guilds carregadas)
        _client.Ready += OnReadyAsync;

        // Slash commands
        _client.SlashCommandExecuted += _slashHandler.HandleSlashCommandAsync;

        // Modais (formulários pop-up)
        _client.ModalSubmitted += _modalHandler.HandleModalSubmittedAsync;

        // Botões
        _client.ButtonExecuted += _buttonHandler.HandleButtonExecutedAsync;
    }

    // ──────────────────────────────────────────────────────────────
    // Event Handlers
    // ──────────────────────────────────────────────────────────────

    private async Task OnReadyAsync()
    {
        _logger.LogInformation(
            "Bot conectado como {Username}#{Discriminator} em {GuildCount} servidor(es).",
            _client.CurrentUser.Username,
            _client.CurrentUser.Discriminator,
            _client.Guilds.Count);

        // Define o status de presença do bot
        await _client.SetGameAsync("Monitorando chamados 🎫", type: ActivityType.Watching);

        // Registra os slash commands em todas as guilds
        await _slashHandler.RegisterCommandsAsync();
    }

    private Task OnLogAsync(LogMessage log)
    {
        var level = log.Severity switch
        {
            LogSeverity.Critical => Microsoft.Extensions.Logging.LogLevel.Critical,
            LogSeverity.Error    => Microsoft.Extensions.Logging.LogLevel.Error,
            LogSeverity.Warning  => Microsoft.Extensions.Logging.LogLevel.Warning,
            LogSeverity.Info     => Microsoft.Extensions.Logging.LogLevel.Information,
            LogSeverity.Verbose  => Microsoft.Extensions.Logging.LogLevel.Debug,
            LogSeverity.Debug    => Microsoft.Extensions.Logging.LogLevel.Trace,
            _                    => Microsoft.Extensions.Logging.LogLevel.Information
        };

        _logger.Log(level, log.Exception, "[Discord.Net] {Message}", log.Message);
        return Task.CompletedTask;
    }
}
