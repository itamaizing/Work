using System.Text;

public static class MenuChatMessageFormatter
{
    public static string FormatMessage(MenuChatChannel channel, MenuChatMessage m, bool senderIsFriend)
    {
        if (m.IsSystem)
            return FormatSystem(m);

        ChatChannelStyle style = G.Colors.Chat.GetStyle(ToChatChannel(channel));

        var sb = new StringBuilder(160);
        sb.Append(FormatTime(m));
        sb.Append(' ');
        sb.Append(FormatUser(channel, m, style, senderIsFriend));
        sb.Append(ChatMessageFormatter.Colorize(":", style.TextColor));
        sb.Append(' ');
        sb.Append(FormatText(m, style));
        sb.Append(FormatStatus(channel, m));
        return sb.ToString();
    }

    private static ChatChannel ToChatChannel(MenuChatChannel channel) =>
        channel.Type == MenuChatChannelType.Private ? ChatChannel.Private : ChatChannel.Group;

    private static string FormatTime(MenuChatMessage m) =>
        ChatMessageFormatter.Colorize(m.Time.ToLocalTime().ToString("HH:mm"), G.Colors.Chat.Time);

    private static string FormatChannel(MenuChatChannel channel, MenuChatMessage m, ChatChannelStyle style)
    {
        string tag = channel.Type == MenuChatChannelType.Private && m.IsMine
            ? $"[{style.Label} → {ChatMessageFormatter.Escape(channel.Title)}]"
            : $"[{style.Label}]";

        return ChatMessageFormatter.Colorize(tag, style.TagColor);
    }

    private static string FormatUser(MenuChatChannel channel, MenuChatMessage m, ChatChannelStyle style, bool senderIsFriend)
    {
        var colors = G.Colors.Chat;
        UnityEngine.Color nameColor = senderIsFriend && !m.IsMine
            ? colors.Friend
            : style.ColorNameByTeam ? colors.Name : style.NameColor;

        return FormatChannel(channel, m, style) + " "
             + ChatMessageFormatter.Colorize(ChatMessageFormatter.Escape(m.SenderLogin), nameColor);
    }

    private static string FormatText(MenuChatMessage m, ChatChannelStyle style) =>
        ChatMessageFormatter.Colorize(ChatRichText.Sanitize(m.Text), style.TextColor);

    private static string FormatStatus(MenuChatChannel channel, MenuChatMessage m)
    {
        var colors = G.Colors.Chat;

        switch (m.State)
        {
            case MenuChatMessageState.Pending:
                return ChatMessageFormatter.Colorize(" …", colors.Pending);

            case MenuChatMessageState.Failed:
                return ChatMessageFormatter.Colorize($" (не доставлено: {ChatMessageFormatter.Escape(m.Error)})", colors.Error);

            default:
                return channel.Type == MenuChatChannelType.Private && m.IsMine && !m.Delivered
                    ? ChatMessageFormatter.Colorize(" (получатель не в сети)", colors.Pending)
                    : string.Empty;
        }
    }

    private static string FormatSystem(MenuChatMessage m) =>
        "<i>" + ChatMessageFormatter.Colorize(ChatMessageFormatter.Escape(m.Text), G.Colors.Chat.SystemMessage) + "</i>";
}