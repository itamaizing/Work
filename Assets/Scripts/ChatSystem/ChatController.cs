using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChatController : NetworkBehaviour
{
    private static readonly string[] WhisperCommands = { "/w", "/ц", "/л" };

    [SerializeField] private ChatConfig _config;
    [SerializeField] private ChatView _view;

    private readonly Dictionary<int, ChatPlayerInfo> _serverPlayers = new();
    
    private readonly List<ChatTarget> _targets = new();
    private int _activeTarget;
    private ChatState _state = ChatState.Hidden;
    private Coroutine _hideCoroutine;
    private int _lastCloseFrame = -1;
    private ChatPlayerInfo _local;
    private bool _hasLocal;
    private GroupManager _groupManager;

    public event Action<ChatMessage> OnMessageReceived;

    private bool HasGroup => _hasLocal && _local.GroupMembers != null && _local.GroupMembers.Length > 0;

    #region Unity

    private void Awake()
    {
        if (_view != null)
        {
            _view.OnMessageSent += HandleMessageSent;
            _view.OnChannelSelected += HandleChannelSelected;
        }

        RebuildTargets();
    }

    private void Update()
    {
        HandleInput();
    }

    private void OnDestroy()
    {
        UnsubscribeFromGroup();

        if (_view != null)
        {
            _view.OnMessageSent -= HandleMessageSent;
            _view.OnChannelSelected -= HandleChannelSelected;
        }
    }

    public override void OnStopClient() => ResetSession();

    public override void OnStopServer() => _serverPlayers.Clear();

    #endregion

    #region Публичный API (клиент)

    public void Initialize(Character localPlayer)
    {
        int teamIndex = localPlayer.NetworkSettings.TeamIndex;
        int userId = MPNetworkManager.Instance != null && MPNetworkManager.Instance.UserID > 0
            ? (int)MPNetworkManager.Instance.UserID
            : (int)localPlayer.netId;

        _local = new ChatPlayerInfo
        {
            UserId = userId,
            Name = ResolveNickname(teamIndex),
            HeroName = localPlayer.Data?.Name ?? "Unknown",
            TeamIndex = teamIndex,
            GroupMembers = ReadGroupMembers()
        };
        _hasLocal = true;

        SubscribeToGroup();
        CmdRegisterSelf(_local);
        RebuildTargets();
    }

    /// Пересобрать состав группы из GroupManager. Вызывается по GroupManager.Changed
    public void RefreshGroup()
    {
        if (!_hasLocal || !NetworkClient.active) return;

        _local.GroupMembers = ReadGroupMembers();
        CmdRegisterSelf(_local);
        RebuildTargets();
    }

    /// <summary>
    /// Открыть личную переписку (например, клик по нику в списке игроков):
    /// добавляет временный канал, делает его активным и открывает чат.
    /// </summary>
    public void OpenPrivateChannel(int peerId, string peerName)
    {
        if (!_hasLocal || peerId == _local.UserId) return;

        EnsurePrivateTarget(peerId, peerName);
        OpenChat(_targets.IndexOf(ChatTarget.ForPeer(peerId, peerName)));
    }

    /// Сброс окна: история и временные (личные) каналы. Вызывается при остановке клиента
    public void ResetSession()
    {
        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
            _hideCoroutine = null;
        }

        UnsubscribeFromGroup();

        _targets.RemoveAll(t => t.Channel == ChatChannel.Private);
        _local = default;
        _hasLocal = false;
        _state = ChatState.Hidden;

        if (_view != null)
        {
            _view.ClearMessages();
            _view.SetState(ChatState.Hidden);
        }

        RebuildTargets();
    }

    // TODO: заменить на реальный логин аккаунта
    private static string ResolveNickname(int teamIndex) => "Player" + teamIndex;

    private static int[] ReadGroupMembers()
    {
        GroupManager groupManager = ServerManager.Instance != null ? ServerManager.Instance.GroupManager : null;
        return groupManager != null ? groupManager.Members.ToArray() : Array.Empty<int>();
    }

    private void SubscribeToGroup()
    {
        UnsubscribeFromGroup();

        _groupManager = ServerManager.Instance != null ? ServerManager.Instance.GroupManager : null;
        if (_groupManager != null)
            _groupManager.Changed += RefreshGroup;
    }

    private void UnsubscribeFromGroup()
    {
        if (_groupManager == null) return;

        _groupManager.Changed -= RefreshGroup;
        _groupManager = null;
    }

    #endregion

    #region Ввод и состояние окна

    private void HandleInput()
    {
        if (_state != ChatState.Focused)
        {
            if (Input.GetKeyDown(KeyCode.Return) && Time.frameCount != _lastCloseFrame)
            {
                bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                int index = shift ? _targets.IndexOf(ChatTarget.Of(ChatChannel.All)) : _activeTarget;
                OpenChat(index);
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseChat();
        }
        else if (Input.GetKeyDown(KeyCode.Tab))
        {
            bool back = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            CycleChannel(back ? -1 : 1);
        }
    }

    private void OpenChat(int targetIndex)
    {
        SetActiveTarget(targetIndex);
        SetState(ChatState.Focused);
        _view.ScrollToBottom();

        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
            _hideCoroutine = null;
        }
    }

    private void CloseChat()
    {
        _lastCloseFrame = Time.frameCount;
        SetState(ChatState.Unfocused);
        StartHideTimer();
    }

    private void SetState(ChatState state)
    {
        _state = state;
        _view.SetState(state);
    }

    private void StartHideTimer()
    {
        if (_hideCoroutine != null)
            StopCoroutine(_hideCoroutine);
        _hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(_config.HideDelay);

        if (_state == ChatState.Unfocused)
            SetState(ChatState.Hidden);

        _hideCoroutine = null;
    }

    #endregion

    #region Каналы

    private void RebuildTargets()
    {
        ChatTarget current = _targets.Count > 0 ? _targets[_activeTarget] : ChatTarget.Of(ChatChannel.Team);
        var privates = _targets.FindAll(t => t.Channel == ChatChannel.Private);

        _targets.Clear();
        _targets.Add(ChatTarget.Of(ChatChannel.All));
        _targets.Add(ChatTarget.Of(ChatChannel.Team));
        if (HasGroup)
            _targets.Add(ChatTarget.Of(ChatChannel.Group));
        _targets.AddRange(privates);

        int index = _targets.IndexOf(current);
        _activeTarget = index >= 0 ? index : _targets.IndexOf(ChatTarget.Of(ChatChannel.Team));

        if (_view != null)
            _view.SetChannels(_targets, _activeTarget);
    }

    private void EnsurePrivateTarget(int peerId, string peerName)
    {
        var target = ChatTarget.ForPeer(peerId, peerName);
        if (_targets.Contains(target)) return;

        _targets.Add(target);
        _view.SetChannels(_targets, _activeTarget);
    }

    private void SetActiveTarget(int index)
    {
        if (index < 0 || index >= _targets.Count) return;

        _activeTarget = index;
        _view.SetActiveChannel(index);
    }

    private void CycleChannel(int step)
    {
        int count = _targets.Count;
        SetActiveTarget(((_activeTarget + step) % count + count) % count);
    }

    private void HandleChannelSelected(int index)
    {
        if (index >= 0 && index < _targets.Count)
            _activeTarget = index;
    }

    #endregion

    #region Отправка

    private void HandleMessageSent(string rawText)
    {
        string text = rawText?.Trim();
        if (string.IsNullOrEmpty(text) || !_hasLocal)
        {
            CloseChat();
            return;
        }

        if (TryParseWhisper(text, out string peerName, out string body))
        {
            if (peerName != null)
                CmdSendMessage(ChatChannel.Private, 0, peerName, body);

            CloseChat();
            return;
        }

        ChatTarget target = _targets[_activeTarget];
        CmdSendMessage(target.Channel, target.PeerId, target.PeerName ?? string.Empty, text);
        CloseChat();
    }

    private static bool TryParseWhisper(string text, out string peerName, out string body)
    {
        peerName = null;
        body = null;

        foreach (string command in WhisperCommands)
        {
            bool isCommand = text.Equals(command, StringComparison.OrdinalIgnoreCase)
                || text.StartsWith(command + " ", StringComparison.OrdinalIgnoreCase);
            if (!isCommand) continue;

            string rest = text.Substring(command.Length).TrimStart();
            int space = rest.IndexOf(' ');
            if (space > 0)
            {
                string message = rest.Substring(space + 1).Trim();
                if (message.Length > 0)
                {
                    peerName = rest.Substring(0, space);
                    body = message;
                }
            }
            return true;
        }

        return false;
    }

    [Command(requiresAuthority = false)]
    private void CmdRegisterSelf(ChatPlayerInfo info, NetworkConnectionToClient sender = null)
    {
        if (sender != null)
            ServerRegisterPlayer(sender, info);
    }
    
    [Server]
    public void ServerRegisterPlayer(NetworkConnectionToClient conn, ChatPlayerInfo info)
    {
        _serverPlayers[conn.connectionId] = info;
    }

    [Command(requiresAuthority = false)]
    private void CmdSendMessage(ChatChannel channel, int recipientId, string recipientName, string text, NetworkConnectionToClient sender = null)
    {
        if (sender == null || !_serverPlayers.TryGetValue(sender.connectionId, out ChatPlayerInfo from)) return;

        text = SanitizeText(text);
        if (text.Length == 0) return;

        var packet = new ChatPacket
        {
            Channel = channel,
            SenderId = from.UserId,
            SenderName = from.Name,
            HeroName = from.HeroName,
            SenderTeam = from.TeamIndex,
            UnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Text = text
        };

        switch (channel)
        {
            case ChatChannel.All:
                ServerDeliver(packet, p => true);
                break;

            case ChatChannel.Team:
                ServerDeliver(packet, p => p.TeamIndex == from.TeamIndex);
                break;

            case ChatChannel.Group:
                if (from.GroupMembers == null || from.GroupMembers.Length == 0) return;

                ServerDeliver(packet, p =>
                    p.UserId == from.UserId ||
                    (ContainsId(from.GroupMembers, p.UserId) && ContainsId(p.GroupMembers, from.UserId)));
                break;

            case ChatChannel.Private:
                bool found = recipientId != 0
                    ? ServerTryFindPlayer(recipientId, out ChatPlayerInfo to)
                    : ServerTryFindPlayerByName(recipientName, out to);
                if (!found) return;

                packet.RecipientId = to.UserId;
                packet.RecipientName = to.Name;
                ServerDeliver(packet, p => p.UserId == to.UserId || p.UserId == from.UserId);
                break;
        }
    }

    private static bool ContainsId(int[] ids, int id) => ids != null && Array.IndexOf(ids, id) >= 0;

    private string SanitizeText(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        text = text.Replace('\n', ' ').Replace('\r', ' ').Trim();
        if (text.Length > _config.MaxMessageLength)
            text = text.Substring(0, _config.MaxMessageLength);
        return text;
    }

    private bool ServerTryFindPlayer(int userId, out ChatPlayerInfo info)
    {
        foreach (var pair in _serverPlayers)
        {
            if (pair.Value.UserId == userId && NetworkServer.connections.ContainsKey(pair.Key))
            {
                info = pair.Value;
                return true;
            }
        }
        info = default;
        return false;
    }

    private bool ServerTryFindPlayerByName(string name, out ChatPlayerInfo info)
    {
        if (!string.IsNullOrEmpty(name))
        {
            foreach (var pair in _serverPlayers)
            {
                if (string.Equals(pair.Value.Name, name, StringComparison.OrdinalIgnoreCase) &&
                    NetworkServer.connections.ContainsKey(pair.Key))
                {
                    info = pair.Value;
                    return true;
                }
            }
        }
        info = default;
        return false;
    }

    private void ServerDeliver(ChatPacket packet, Func<ChatPlayerInfo, bool> filter)
    {
        List<int> disconnected = null;

        foreach (var pair in _serverPlayers)
        {
            if (!NetworkServer.connections.TryGetValue(pair.Key, out NetworkConnectionToClient conn))
            {
                (disconnected ??= new List<int>()).Add(pair.Key);
                continue;
            }

            if (filter(pair.Value))
                TargetReceivePacket(conn, packet);
        }

        if (disconnected != null)
            foreach (int id in disconnected)
                _serverPlayers.Remove(id);
    }

    #endregion

    #region Приём

    [TargetRpc]
    private void TargetReceivePacket(NetworkConnectionToClient target, ChatPacket packet)
    {
        HandlePacket(packet);
    }

    private void HandlePacket(ChatPacket packet)
    {
        if (!_hasLocal) return;

        var message = new ChatMessage(packet, _local.UserId);

        if (message.Channel == ChatChannel.Private)
            EnsurePrivateTarget(message.PeerId, message.PeerName);

        _view.AddMessage(message);
        OnMessageReceived?.Invoke(message);

        if (_state != ChatState.Focused)
        {
            SetState(ChatState.Unfocused);
            StartHideTimer();
        }

        _view.ScrollToBottom();
    }

    #endregion
}