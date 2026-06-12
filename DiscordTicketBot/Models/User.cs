namespace DiscordTicketBot.Models;

/// <summary>
/// Representa um usuário Discord registrado no sistema.
/// </summary>
public class User
{
    public int Id { get; set; }

    /// <summary>ID único do usuário no Discord (ulong convertido para string para SQLite)</summary>
    public string DiscordUserId { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Relacionamentos
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
