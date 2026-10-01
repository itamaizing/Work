using System;

[Serializable]
public class ChatMessage
{
    public string PlayerName { get; private set; }
    public string HeroName { get; private set; }
    public int TeamIndex { get; private set; }
    public ChatChannel Channel { get; private set; }
    public string MessageText { get; private set; }
    public string Time { get; private set; }

    public ChatMessage(string playerName, string heroName, int teamIndex, ChatChannel channel, string messageText)
    {
        PlayerName = playerName;
        HeroName = heroName;
        TeamIndex = teamIndex;
        Channel = channel;
        MessageText = messageText;
        Time = DateTime.Now.ToString("HH:mm");
    }

    public string GetFormattedMessage()
    {
        string channelText = Channel == ChatChannel.Team ? "Команда" : "Общий";
        return $"{Time} [{channelText}] {PlayerName} ({HeroName}): {MessageText}";
    }
}
