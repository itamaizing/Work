using TMPro;
using UnityEngine;

public class ChatMessageUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _messageText;

    public void Setup(ChatMessage message, Color teamColor, Color whiteColor)
    {
        if (_messageText == null) return;

        string channelText = message.Channel == ChatChannel.Team ? "Команда" : "Общий";
        Color channelColor = message.Channel == ChatChannel.Team ? teamColor : whiteColor;

        string formattedMessage = 
            $"<color=#{ColorUtility.ToHtmlStringRGB(whiteColor)}>{message.Time}</color> " +
            $"<color=#{ColorUtility.ToHtmlStringRGB(channelColor)}>[{channelText}]</color> " +
            $"<color=#{ColorUtility.ToHtmlStringRGB(teamColor)}>{message.PlayerName} ({message.HeroName})</color>: " +
            $"<color=#{ColorUtility.ToHtmlStringRGB(whiteColor)}>{message.MessageText}</color>";

        _messageText.text = formattedMessage;
    }
}
