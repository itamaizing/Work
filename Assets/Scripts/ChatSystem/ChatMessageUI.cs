using TMPro;
using UnityEngine;

public class ChatMessageUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _messageText;

    public void SetText(string formattedText)
    {
        if (_messageText != null)
            _messageText.text = formattedText;
    }
}