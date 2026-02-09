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
    [SerializeField] private Transform _unfocusedMessageContainer;
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private TMP_Dropdown _channelDropdown;
    [SerializeField] private ChatMessageUI _messagePrefab;

    [Header("Settings")]
    [SerializeField] private ChatConfig _config;

    public event Action<string, ChatChannel> OnMessageSent;

    private List<ChatMessageUI> _focusedMessageUIList = new List<ChatMessageUI>();
    private List<ChatMessageUI> _unfocusedMessageUIList = new List<ChatMessageUI>();
    private List<ChatMessage> _allMessages = new List<ChatMessage>();
    private ChatChannel _currentChannel = ChatChannel.Team;

    private void Awake()
    {
        SetupInputField();
        SetupChannelDropdown();
        
        SetState(ChatState.Hidden);
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

        _allMessages.Add(message);

        ChatMessageUI focusedMessageUI = Instantiate(_messagePrefab, _messageContainer);
        focusedMessageUI.Setup(message, GetTeamColor(message.TeamIndex), _config.WhiteColor);
        _focusedMessageUIList.Add(focusedMessageUI);

        UpdateUnfocusedMessages();
    }

    public void ClearMessages()
    {
        _allMessages.Clear();

        foreach (var messageUI in _focusedMessageUIList)
        {
            if (messageUI != null)
            {
                Destroy(messageUI.gameObject);
            }
        }
        _focusedMessageUIList.Clear();

        foreach (var messageUI in _unfocusedMessageUIList)
        {
            if (messageUI != null)
            {
                Destroy(messageUI.gameObject);
            }
        }
        _unfocusedMessageUIList.Clear();
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
        foreach (var messageUI in _unfocusedMessageUIList)
        {
            if (messageUI != null)
            {
                Destroy(messageUI.gameObject);
            }
        }
        _unfocusedMessageUIList.Clear();

        int startIndex = Mathf.Max(0, _allMessages.Count - _config.MaxUnfocusedMessages);
        
        for (int i = startIndex; i < _allMessages.Count; i++)
        {
            if (_unfocusedMessageContainer != null)
            {
                ChatMessageUI unfocusedMessageUI = Instantiate(_messagePrefab, _unfocusedMessageContainer);
                unfocusedMessageUI.Setup(_allMessages[i], GetTeamColor(_allMessages[i].TeamIndex), _config.WhiteColor);
                _unfocusedMessageUIList.Add(unfocusedMessageUI);
            }
        }
    }

    private void OnSubmit(string text)
    {
        OnMessageSent?.Invoke(text, _currentChannel);
        _inputField.text = string.Empty;
    }

    private void OnChannelChanged(int value)
    {
        _currentChannel = (ChatChannel)value;
    }

    private Color GetTeamColor(int teamIndex)
    {
        return teamIndex == 1 ? _config.Team1Color : _config.Team2Color;
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
