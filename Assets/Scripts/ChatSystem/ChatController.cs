using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChatController : NetworkBehaviour
{
    [SerializeField] private ChatConfig _config;
    [SerializeField] private ChatView _view;

    private ChatState _currentState = ChatState.Hidden;
    private ChatChannel _currentChannel = ChatChannel.Team;
    private List<ChatMessage> _messages = new List<ChatMessage>();
    private Coroutine _hideCoroutine;
    
    private Character _localPlayer;
    private int _localTeamIndex;

    public event Action<ChatMessage> OnMessageReceived;

    private void Awake()
    {
        if (_view != null)
        {
            _view.OnMessageSent += HandleMessageSent;
        }
    }

    private void Update()
    {
        HandleInput();
    }

    public void Initialize(Character localPlayer)
    {
        Debug.LogError("Player name is: " + localPlayer.name);
        _localPlayer = localPlayer;
        _localTeamIndex = localPlayer.NetworkSettings.TeamIndex;
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Return) && _currentState != ChatState.Focused)
        {
            OpenChat(_currentChannel);
        }
        else if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Return) && _currentState != ChatState.Focused)
        {
            OpenChat(ChatChannel.All);
        }
        else if (Input.GetKeyDown(KeyCode.Escape) && _currentState == ChatState.Focused)
        {
            CloseChat();
        }
        else if (Input.GetKeyDown(KeyCode.Tab) && _currentState == ChatState.Focused)
        {
            CycleChannel();
        }
    }

    private void OpenChat(ChatChannel channel)
    {
        _currentChannel = channel;
        SetState(ChatState.Focused);
        _view.SetCurrentChannel(_currentChannel);
        _view.ScrollToBottom();
        
        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
            _hideCoroutine = null;
        }
    }

    private void CloseChat()
    {
        SetState(ChatState.Unfocused);
        StartHideTimer();
    }

    private void CycleChannel()
    {
        _currentChannel = _currentChannel == ChatChannel.Team ? ChatChannel.All : ChatChannel.Team;
        _view.SetCurrentChannel(_currentChannel);
    }

    private void HandleMessageSent(string messageText, ChatChannel channel)
    {
        if (string.IsNullOrWhiteSpace(messageText))
        {
            CloseChat();
            return;
        }
        
        if (messageText.Length > _config.MaxMessageLength)
        {
            messageText = messageText.Substring(0, _config.MaxMessageLength);
        }

        messageText = messageText.Replace("\n", " ").Replace("\r", " ");

        SendMessageToServer(messageText, channel);
        CloseChat();
    }

    private void SendMessageToServer(string messageText, ChatChannel channel)
    {
        if (_localPlayer == null) return;

        string playerName = "Player" + _localTeamIndex;
        string heroName = _localPlayer.Data?.Name ?? "Unknown";

        var networkIdentity = _localPlayer.GetComponent<NetworkIdentity>();
        if (networkIdentity != null && _localPlayer.isOwned)
        {
            CmdSendMessage(playerName, heroName, _localTeamIndex, channel, messageText);
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdSendMessage(string playerName, string heroName, int teamIndex, ChatChannel channel, string messageText, NetworkConnectionToClient sender = null)
    {
        RpcReceiveMessage(playerName, heroName, teamIndex, channel, messageText);
    }

    [ClientRpc]
    private void RpcReceiveMessage(string playerName, string heroName, int teamIndex, ChatChannel channel, string messageText)
    {
        ReceiveMessage(playerName, heroName, teamIndex, channel, messageText);
    }

    public void ReceiveMessage(string playerName, string heroName, int teamIndex, ChatChannel channel, string messageText)
    {
        if (channel == ChatChannel.Team && teamIndex != _localTeamIndex)
        {
            return;
        }

        ChatMessage message = new ChatMessage(playerName, heroName, teamIndex, channel, messageText);
        _messages.Add(message);
        _view.AddMessage(message);

        OnMessageReceived?.Invoke(message);

        if (_currentState != ChatState.Focused)
        {
            SetState(ChatState.Unfocused);
            StartHideTimer();
        }

        _view.ScrollToBottom();
    }

    private void SetState(ChatState newState)
    {
        _currentState = newState;
        _view.SetState(newState);
    }

    private void StartHideTimer()
    {
        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
        }
        _hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(_config.HideDelay);
        
        if (_currentState == ChatState.Unfocused)
        {
            SetState(ChatState.Hidden);
        }
        
        _hideCoroutine = null;
    }

    private void OnDestroy()
    {
        if (_view != null)
        {
            _view.OnMessageSent -= HandleMessageSent;
        }
    }
}
