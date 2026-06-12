using Discord;

namespace DiscordTicketBot.Components;

public static class TicketComponents
{
    public const string PausePrefix   = "ticket_pause_";
    public const string ResumePrefix  = "ticket_resume_";
    public const string FinishPrefix  = "ticket_finish_";

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

    public static MessageComponent BuildFinishedComponents()
    {
        return new ComponentBuilder()
            .WithButton(
                label: "Chamado Finalizado",
                customId: "ticket_done",
                style: ButtonStyle.Secondary,
                emote: new Emoji("🏁"),
                disabled: true)
            .Build();
    }
}
