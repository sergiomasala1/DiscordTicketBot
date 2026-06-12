using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using DiscordTicketBot.Commands;
using DiscordTicketBot.Database;
using DiscordTicketBot.Services;

// ──────────────────────────────────────────────────────────────────
// Configuração do Serilog (logs em console + arquivo)
// ──────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/bot-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Iniciando DiscordTicketBot...");

    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureAppConfiguration((ctx, config) =>
        {
            config
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();
        })
        .ConfigureServices((ctx, services) =>
        {
            var config = ctx.Configuration;

            // ── Discord.Net ──────────────────────────────────────
            var socketConfig = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages,
                LogLevel = LogSeverity.Info,
                MessageCacheSize = 100,
                AlwaysDownloadUsers = false
            };

            services.AddSingleton(socketConfig);
            services.AddSingleton<DiscordSocketClient>();

            // ── Entity Framework + SQLite ────────────────────────
            var dbPath = config["Database:Path"] ?? "ticketbot.db";

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}")
                       .EnableSensitiveDataLogging(false));

            // ── Serviços de domínio ──────────────────────────────
            services.AddScoped<TicketService>();
            services.AddSingleton<DatabaseInitializer>();

            // ── Handlers de comandos ─────────────────────────────
            services.AddSingleton<SlashCommandHandler>();
            services.AddSingleton<ModalHandler>();
            services.AddSingleton<ButtonHandler>();

            // ── Serviço hospedado principal ──────────────────────
            services.AddHostedService<BotService>();
        })
        .Build();

    // Resolve dependências scoped necessárias antes do host iniciar
    using (var scope = host.Services.CreateScope())
    {
        // Garante que o banco é criado ao iniciar
        var dbInit = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
        await dbInit.InitializeAsync();
    }

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Bot encerrado com exceção não tratada.");
}
finally
{
    Log.CloseAndFlush();
}
