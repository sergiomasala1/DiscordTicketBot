using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiscordTicketBot.Database;

/// <summary>
/// Responsável por aplicar migrations e garantir que o banco exista ao iniciar.
/// </summary>
public class DatabaseInitializer
{
    private readonly AppDbContext _context;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(AppDbContext context, ILogger<DatabaseInitializer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Inicializando banco de dados SQLite...");

            // Garante que o banco foi criado e todas as migrations aplicadas
            await _context.Database.EnsureCreatedAsync();

            _logger.LogInformation("Banco de dados inicializado com sucesso.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao inicializar banco de dados.");
            throw;
        }
    }
}
