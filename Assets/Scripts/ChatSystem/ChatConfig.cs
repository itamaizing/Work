using UnityEngine;

[CreateAssetMenu(fileName = "ChatConfig", menuName = "Game/Chat Config")]
public class ChatConfig : ScriptableObject
{
    [Header("Message Settings")]
    [SerializeField] private int _maxMessageLength = 80;
    
    [Header("UI Settings")]
    [SerializeField] private int _maxUnfocusedMessages = 3;
    [SerializeField] private float _hideDelay = 5f;

    public int MaxMessageLength => _maxMessageLength;
    public int MaxUnfocusedMessages => _maxUnfocusedMessages;
    public float HideDelay => _hideDelay;
}
