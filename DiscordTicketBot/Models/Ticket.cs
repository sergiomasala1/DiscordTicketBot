namespace DiscordTicketBot.Models;

/// <summary>
/// Status possíveis de um chamado.
/// </summary>
public enum TicketStatus
{
    Active = 0,
    Paused = 1,
    Finished = 2
}

/// <summary>
/// Representa um chamado de suporte com controle de tempo.
/// </summary>
public class Ticket
{
    public int Id { get; set; }

    /// <summary>Número do chamado informado pelo técnico</summary>
    public string TicketNumber { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TicketStatus Status { get; set; } = TicketStatus.Active;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? FinishedAt { get; set; }

    /// <summary>Tempo total pausado em segundos</summary>
    public long TotalPausedSeconds { get; set; } = 0;

    /// <summary>ID da mensagem Discord que contém o embed do chamado (para atualização)</summary>
    public string? DiscordMessageId { get; set; }

    /// <summary>ID do canal Discord onde o embed foi enviado</summary>
    public string? DiscordChannelId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Relacionamentos
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public ICollection<TimeSession> TimeSessions { get; set; } = new List<TimeSession>();
    public ICollection<PauseSession> PauseSessions { get; set; } = new List<PauseSession>();

    // ──────────────────────────────────────────────────────────────
    // Propriedades calculadas
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Calcula o tempo total decorrido desde o início (incluindo pausas).
    /// </summary>
    public TimeSpan TotalElapsed
    {
        get
        {
            var end = FinishedAt ?? DateTime.UtcNow;
            return end - StartedAt;
        }
    }

    /// <summary>
    /// Calcula o tempo líquido trabalhado (descontando pausas).
    /// </summary>
    public TimeSpan NetElapsed
    {
        get
        {
            var totalPaused = TimeSpan.FromSeconds(TotalPausedSeconds);

            // Se estiver pausado agora, inclui o tempo da pausa atual
            var activePause = PauseSessions.FirstOrDefault(p => p.ResumedAt == null);
            if (activePause != null)
                totalPaused += DateTime.UtcNow - activePause.PausedAt;

            var net = TotalElapsed - totalPaused;
            return net < TimeSpan.Zero ? TimeSpan.Zero : net;
        }
    }

    /// <summary>
    /// Formata o tempo líquido como string legível (ex: 1h 23m 45s).
    /// </summary>
    public string NetElapsedFormatted => FormatTimeSpan(NetElapsed);

    /// <summary>
    /// Formata o tempo total como string legível.
    /// </summary>
    public string TotalElapsedFormatted => FormatTimeSpan(TotalElapsed);

    private static string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes:D2}m {ts.Seconds:D2}s";
        if (ts.TotalMinutes >= 1)
            return $"{ts.Minutes}m {ts.Seconds:D2}s";
        return $"{ts.Seconds}s";
    }
}
