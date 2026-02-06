using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatView : MonoBehaviour, IChatView
{
    [Header("UI References")]
    [SerializeField] private GameObject _focusedPanel;
    [SerializeField] private GameObject _unfocusedPanel;
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private Transform _messageContainer;
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private TMP_Dropdown _channelDropdown;
    [SerializeField] private ChatMessageUI _messagePrefab;

    [Header("Settings")]
    [SerializeField] private ChatConfig _config;
    [SerializeField] private Color _team1Color = Color.blue;
    [SerializeField] private Color _team2Color = Color.red;
    [SerializeField] private Color _whiteColor = Color.white;

    public event Action<string, ChatChannel> OnMessageSent;

    private List<ChatMessageUI> _messageUIList = new List<ChatMessageUI>();
    private ChatChannel _currentChannel = ChatChannel.Team;

    private void Awake()
    {
        SetupInputField();
        SetupChannelDropdown();
    }

    private void SetupInputField()
    {
        if (_inputField != null)
        {
            _inputField.characterLimit = _config.MaxMessageLength;
            _inputField.lineType = TMP_InputField.LineType.SingleLine;
            
            _inputField.onSubmit.AddListener(OnSubmit);
        }
    }

    private void SetupChannelDropdown()
    {
        if (_channelDropdown != null)
        {
            _channelDropdown.ClearOptions();
            _channelDropdown.AddOptions(new List<string> { "Союзники", "Общий" });
            _channelDropdown.onValueChanged.AddListener(OnChannelChanged);
        }
    }

    public void SetState(ChatState state)
    {
        switch (state)
        {
            case ChatState.Hidden:
                _focusedPanel.SetActive(false);
                _unfocusedPanel.SetActive(false);
                break;
            
            case ChatState.Unfocused:
                _focusedPanel.SetActive(false);
                _unfocusedPanel.SetActive(true);
                UpdateUnfocusedMessages();
                break;
            
            case ChatState.Focused:
                _focusedPanel.SetActive(true);
                _unfocusedPanel.SetActive(false);
                _inputField.ActivateInputField();
                _inputField.Select();
                break;
        }
    }

    public void AddMessage(ChatMessage message)
    {
        if (_messagePrefab == null || _messageContainer == null) return;

        ChatMessageUI messageUI = Instantiate(_messagePrefab, _messageContainer);
        messageUI.Setup(message, GetTeamColor(message.TeamIndex), _whiteColor);
        _messageUIList.Add(messageUI);
    }

    public void ClearMessages()
    {
        foreach (var messageUI in _messageUIList)
        {
            if (messageUI != null)
            {
                Destroy(messageUI.gameObject);
            }
        }
        _messageUIList.Clear();
    }

    public void SetCurrentChannel(ChatChannel channel)
    {
        _currentChannel = channel;
        _channelDropdown.value = (int)channel;
    }

    public void ScrollToBottom()
    {
        if (_scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void UpdateUnfocusedMessages()
    {
        int startIndex = Mathf.Max(0, _messageUIList.Count - _config.MaxUnfocusedMessages);
        
        for (int i = 0; i < _messageUIList.Count; i++)
        {
            if (_messageUIList[i] != null)
            {
                _messageUIList[i].gameObject.SetActive(i >= startIndex);
            }
        }
    }

    private void OnSubmit(string text)
    {
        OnMessageSent?.Invoke(text, _currentChannel);
        Debug.LogError("Отправлено сообщение: " + text);
        _inputField.text = string.Empty;
    }

    private void OnChannelChanged(int value)
    {
        _currentChannel = (ChatChannel)value;
    }

    private Color GetTeamColor(int teamIndex)
    {
        return teamIndex == 1 ? _team1Color : _team2Color;
    }

    private void OnDestroy()
    {
        if (_inputField != null)
        {
            _inputField.onSubmit.RemoveListener(OnSubmit);
        }
        
        if (_channelDropdown != null)
        {
            _channelDropdown.onValueChanged.RemoveListener(OnChannelChanged);
        }
    }
}

public enum ChatChannel
{
    Team,
    All
}

public enum ChatState
{
    Hidden,
    Unfocused,
    Focused
}
