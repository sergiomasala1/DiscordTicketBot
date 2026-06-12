using Discord;

namespace DiscordTicketBot.Components;

/// <summary>
/// Fábrica de componentes (botões) para ações nos chamados.
/// Cada botão carrega o ID do chamado via CustomId para rastreamento.
/// </summary>
public static class TicketComponents
{
    // ── Prefixos para identificar ações nos botões ──────────────
    public const string PausePrefix   = "ticket_pause_";
    public const string ResumePrefix  = "ticket_resume_";
    public const string FinishPrefix  = "ticket_finish_";

    /// <summary>
    /// Retorna o conjunto de botões para um chamado ativo.
    /// </summary>
    public static MessageComponent BuildActiveComponents(int ticketId)
    {
        return new ComponentBuilder()
            .WithButton(
                label: "Pausar",
                customId: $"{PausePrefix}{ticketId}",
                style: ButtonStyle.Secondary,
                emote: new Emoji("⏸️"))
            .WithButton(
                label: "Finalizar",
                customId: $"{FinishPrefix}{ticketId}",
                style: ButtonStyle.Danger,
                emote: new Emoji("✅"))
            .Build();
    }

    /// <summary>
    /// Retorna o conjunto de botões para um chamado pausado.
    /// </summary>
    public static MessageComponent BuildPausedComponents(int ticketId)
    {
        return new ComponentBuilder()
            .WithButton(
                label: "Retomar",
                customId: $"{ResumePrefix}{ticketId}",
                style: ButtonStyle.Success,
                emote: new Emoji("▶️"))
            .WithButton(
                label: "Finalizar",
                customId: $"{FinishPrefix}{ticketId}",
                style: ButtonStyle.Danger,
                emote: new Emoji("✅"))
            .Build();
    }

    /// <summary>
    /// Retorna os componentes desabilitados para um chamado finalizado.
    /// </summary>
    public static MessageComponent BuildFinishedComponents()
    {
        return new ComponentBuilder()
            .WithButton(
                label: "Chamado Finalizado",
                customId: "ticket_done",
                style: ButtonStyle.Secondary,
                emote: new Emoji("🏁"),
                isDisabled: true)
            .Build();
    }
}
