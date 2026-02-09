using UnityEngine;

[CreateAssetMenu(fileName = "ChatConfig", menuName = "Game/Chat Config")]
public class ChatConfig : ScriptableObject
{
    [Header("Message Settings")]
    [SerializeField] private int _maxMessageLength = 80;
    
    [Header("UI Settings")]
    [SerializeField] private int _maxUnfocusedMessages = 3;
    [SerializeField] private float _hideDelay = 5f;
    
    [Header("Colors")]
    [SerializeField] private Color _team1Color = Color.blue;
    [SerializeField] private Color _team2Color = Color.red;
    [SerializeField] private Color _whiteColor = Color.white;

    public int MaxMessageLength => _maxMessageLength;
    public int MaxUnfocusedMessages => _maxUnfocusedMessages;
    public float HideDelay => _hideDelay;

    public Color Team1Color => _team1Color;
    public Color Team2Color => _team2Color;
    public Color WhiteColor => _whiteColor;
}
