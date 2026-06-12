using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DiscordTicketBot.Commands;
using DiscordTicketBot.Database;

namespace DiscordTicketBot.Services;

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

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _dbInit.InitializeAsync();

        RegisterEvents();

        // Lê token: primeiro tenta variável de ambiente, depois appsettings
        var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");

        if (string.IsNullOrWhiteSpace(token))
            token = _config["Discord:Token"];

        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException(
                "Token do Discord não encontrado! Configure a variável de ambiente DISCORD_TOKEN no Railway.");

        _logger.LogInformation("Token encontrado, conectando ao Discord...");

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Bot Discord encerrando...");
        await _client.StopAsync();
        await _client.LogoutAsync();
    }

    private void RegisterEvents()
    {
        _client.Log += OnLogAsync;
        _client.Ready += OnReadyAsync;
        _client.SlashCommandExecuted += _slashHandler.HandleSlashCommandAsync;
        _client.ModalSubmitted += _modalHandler.HandleModalSubmittedAsync;
        _client.ButtonExecuted += _buttonHandler.HandleButtonExecutedAsync;
    }

    private async Task OnReadyAsync()
    {
        _logger.LogInformation(
            "Bot conectado como {Username} em {GuildCount} servidor(es).",
            _client.CurrentUser.Username,
            _client.Guilds.Count);

        await _client.SetGameAsync("Monitorando chamados 🎫", type: ActivityType.Watching);
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
