using System;
using System.Text;
using UnityEngine;

public static class ChatMessageFormatter
{
    public static string Format(ChatMessage message)
    {
        ChatChannelStyle style = G.Colors.Chat.GetStyle(message.Channel);

        var sb = new StringBuilder(160);
        sb.Append(FormatTime(message));
        sb.Append(' ');
        sb.Append(FormatUser(message, style));
        sb.Append(Colorize(":", style.TextColor));
        sb.Append(' ');
        sb.Append(FormatText(message, style));
        return sb.ToString();
    }

    private static string FormatTime(ChatMessage m)
    {
        string time = DateTimeOffset.FromUnixTimeSeconds(m.UnixTime).ToLocalTime().ToString("HH:mm");
        return Colorize(time, G.Colors.Chat.Time);
    }

    private static string FormatChannel(ChatMessage m, ChatChannelStyle style)
    {
        string tag = m.Channel == ChatChannel.Private && m.IsOwn
            ? $"[{style.Label} → {Escape(m.RecipientName)}]"
            : $"[{style.Label}]";

        return Colorize(tag, style.TagColor);
    }

    private static string FormatUser(ChatMessage m, ChatChannelStyle style)
    {
        Color nameColor = style.ColorNameByTeam ? TeamColor(m.SenderTeam) : style.NameColor;
        string user = string.IsNullOrEmpty(m.HeroName)
            ? Escape(m.SenderName)
            : $"({Escape(m.HeroName)}) {Escape(m.SenderName)}";

        return FormatChannel(m, style) + " " + Colorize(user, nameColor);
    }

    private static string FormatText(ChatMessage m, ChatChannelStyle style) =>
        Colorize(ChatRichText.Sanitize(m.Text), style.TextColor);

    private static Color TeamColor(int teamIndex) =>
        teamIndex == 1 ? G.Colors.Chat.Team1 : G.Colors.Chat.Team2;

    public static string Colorize(string text, Color color) =>
        $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";

    public static string Escape(string text) =>
        string.IsNullOrEmpty(text) ? string.Empty : text.Replace("<", "<\u200B");
}