namespace DiscordTicketBot.Models;

/// <summary>
/// Representa uma sessão de trabalho ativo em um chamado.
/// Cada vez que o técnico inicia ou retoma, uma nova sessão é criada.
/// </summary>
public class TimeSession
{
    public int Id { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? EndedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Relacionamentos
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    /// <summary>
    /// Duração da sessão. Se ainda ativa, usa o tempo atual.
    /// </summary>
    public TimeSpan Duration => (EndedAt ?? DateTime.UtcNow) - StartedAt;
}

/// <summary>
/// Representa uma sessão de pausa em um chamado.
/// </summary>
public class PauseSession
{
    public int Id { get; set; }

    public DateTime PausedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Null enquanto a pausa ainda estiver ativa</summary>
    public DateTime? ResumedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Relacionamentos
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    /// <summary>
    /// Duração da pausa. Se ainda ativa, usa o tempo atual.
    /// </summary>
    public TimeSpan Duration => (ResumedAt ?? DateTime.UtcNow) - PausedAt;
}
