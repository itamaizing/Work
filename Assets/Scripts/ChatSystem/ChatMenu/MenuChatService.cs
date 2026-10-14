using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class MenuChatService : MonoBehaviour
{
    public const int MaxMessageLength = 300;
    private const int MaxCachedMessages = 200;
    private const int HistoryPageSize = 50;

    private const string MSG_CHAT = "chatMessage";
    private const string MSG_CHAT_ERROR = "chatError";
    private const string MSG_FRIEND_UI = "needUpdateFriendUI";
    private const string MSG_GET_ONLINE = "getOnlineUsers";
    private const string MSG_ONLINE_USERS = "onlineUsers";
    private const string MSG_USER_ONLINE = "userOnline";
    private const string MSG_USER_OFFLINE = "userOffline";

    public static MenuChatService Instance { get; private set; }

    public event Action ChannelsChanged;
    public event Action<MenuChatChannel> ActiveChanged;
    public event Action<MenuChatChannel> FocusChanged;
    public event Action<MenuChatChannel, MenuChatMessage> MessageAdded;
    public event Action<MenuChatChannel> HistoryLoaded;

    public MenuChatChannel Active { get; private set; }
    public MenuChatChannel Focus { get; private set; }

    private readonly Dictionary<string, MenuChatChannel> _channels = new Dictionary<string, MenuChatChannel>();
    private readonly HashSet<int> _friendIds = new HashSet<int>();
    private readonly Dictionary<int, string> _onlineUsers = new Dictionary<int, string>();
    private MenuChatChannel _groupChannel;
    private GroupManager _hookedGroupManager;
    private bool _initialized;

    private int MyId => MPNetworkManager.Instance.UserID;
    
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject);
    }

    private void Start()
    {
        if (Instance != this) return;
        WebSocketClient.Instance.MessageReceived += OnMessageReceived;
        WebSocketClient.Instance.Disconnected += OnSocketDisconnected;
        WebSocketClient.Instance.Reconnected += OnSocketReconnected;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        if (WebSocketClient.Instance != null)
        {
            WebSocketClient.Instance.MessageReceived -= OnMessageReceived;
            WebSocketClient.Instance.Disconnected -= OnSocketDisconnected;
            WebSocketClient.Instance.Reconnected -= OnSocketReconnected;
        }
        if (_hookedGroupManager != null)
            _hookedGroupManager.Changed -= OnGroupChanged;
    }

    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        if (_groupChannel == null)
        {
            _groupChannel = new MenuChatChannel(MenuChatChannelType.Group, 0, "Текущая группа");
            _channels[_groupChannel.Key] = _groupChannel;
        }

        HookGroupManager();

        if (Active == null)
        {
            Active = _groupChannel;
            Focus = _groupChannel;
        }

        if (MyId >= 0)
        {
            RefreshFriendsAndConversations();
            RequestOnlineUsers();
        }

        ChannelsChanged?.Invoke();
    }

    public void ResetState()
    {
        _initialized = false;
        _channels.Clear();
        _friendIds.Clear();
        _onlineUsers.Clear();
        _groupChannel = null;
        Active = null;
        Focus = null;
        ChannelsChanged?.Invoke();
    }

    private void OnSocketDisconnected()
    {
        _onlineUsers.Clear();
        AddSystem(Active, "Соединение с сервером потеряно, переподключаюсь...");
        ChannelsChanged?.Invoke();
    }

    private void OnSocketReconnected()
    {
        AddSystem(Active, "Соединение восстановлено");
        if (_initialized && MyId >= 0)
            RefreshFriendsAndConversations();
    }

    private void HookGroupManager()
    {
        if (_hookedGroupManager != null || ServerManager.Instance == null) return;
        _hookedGroupManager = ServerManager.Instance.GroupManager;
        if (_hookedGroupManager != null)
            _hookedGroupManager.Changed += OnGroupChanged;
    }

    private bool IsInGroup
    {
        get
        {
            if (ServerManager.Instance == null) return false;
            var gm = ServerManager.Instance.GroupManager;
            return gm != null && (gm.Members.Count > 0 || gm.HostId != null);
        }
    }

    private void OnGroupChanged()
    {
        if (!IsInGroup && _groupChannel != null && _groupChannel.Messages.Count > 0)
        {
            _groupChannel.Messages.Clear();
            _groupChannel.Unread = 0;
            if (Active == _groupChannel) ActiveChanged?.Invoke(Active);
            ChannelsChanged?.Invoke();
        }
    }
    
    public IEnumerable<MenuChatChannel> GetChannels(MenuChatGroup group)
    {
        return _channels.Values
            .Where(c => IsVisible(c) && GroupOf(c) == group)
            .OrderByDescending(c => IsOnline(c))
            .ThenBy(c => c.Title, StringComparer.CurrentCultureIgnoreCase);
    }

    public bool IsOnline(MenuChatChannel c)
    {
        return c.Type == MenuChatChannelType.Private && _onlineUsers.ContainsKey(c.UserId);
    }

    private bool IsVisible(MenuChatChannel c)
    {
        if (c.Type == MenuChatChannelType.Group) return true;
        if (c.UserId == MyId) return false;
        if (c == Active) return true;
        return _friendIds.Contains(c.UserId)
               || c.HasConversation
               || c.Messages.Count > 0
               || _onlineUsers.ContainsKey(c.UserId);
    }

    private void RequestOnlineUsers()
    {
        if (WebSocketClient.Instance != null && WebSocketClient.Instance.IsConnected)
            WebSocketClient.Instance.SendMessageToServer(JsonConvert.SerializeObject(new { type = MSG_GET_ONLINE }));
    }

    private void HandleOnlineSnapshot(Dictionary<string, string> d)
    {
        List<UserDto> users = null;
        try { users = JsonConvert.DeserializeObject<List<UserDto>>(Get(d, "users")); }
        catch (JsonException e) { Debug.LogError("Chat: online users parse error " + e.Message); }
        if (users == null) return;

        _onlineUsers.Clear();
        foreach (var u in users)
        {
            if (u.id == MyId) continue;
            _onlineUsers[u.id] = u.login;
            GetOrCreatePrivate(u.id, u.login);
        }
        ChannelsChanged?.Invoke();
    }

    private void HandleUserOnline(Dictionary<string, string> d)
    {
        if (!int.TryParse(Get(d, "playerId"), out int id) || id == MyId) return;
        string login = Get(d, "login");
        _onlineUsers[id] = login;
        GetOrCreatePrivate(id, login);
        ChannelsChanged?.Invoke();
    }

    private void HandleUserOffline(Dictionary<string, string> d)
    {
        if (!int.TryParse(Get(d, "playerId"), out int id)) return;
        if (_onlineUsers.Remove(id))
            ChannelsChanged?.Invoke();
    }

    public MenuChatGroup GroupOf(MenuChatChannel c)
    {
        if (c.Type == MenuChatChannelType.Group) return MenuChatGroup.Channels;
        return _friendIds.Contains(c.UserId) ? MenuChatGroup.Friends : MenuChatGroup.Users;
    }

    private MenuChatChannel GetOrCreatePrivate(int userId, string login)
    {
        string k = MenuChatChannel.PrivateKey(userId);
        if (!_channels.TryGetValue(k, out var ch))
        {
            ch = new MenuChatChannel(MenuChatChannelType.Private, userId, string.IsNullOrEmpty(login) ? "Игрок #" + userId : login);
            _channels[k] = ch;
        }
        else if (!string.IsNullOrEmpty(login))
        {
            ch.Title = login;
        }
        return ch;
    }

    public void OpenPrivate(int userId, string login)
    {
        if (!_initialized) Initialize();
        var ch = GetOrCreatePrivate(userId, login);
        ChannelsChanged?.Invoke();
        SetActive(ch);
    }

    public void SetActive(MenuChatChannel ch)
    {
        if (ch == null) return;
        Active = ch;
        ch.Unread = 0;

        if (ch.Type == MenuChatChannelType.Private && !ch.HistoryRequested)
            LoadHistory(ch);

        ActiveChanged?.Invoke(ch);
        SetFocus(ch);
        ChannelsChanged?.Invoke();
    }

    public void SetFocus(MenuChatChannel ch)
    {
        if (ch == null) return;
        Focus = ch;
        FocusChanged?.Invoke(ch);
    }

    public bool Send(string text)
    {
        if (Focus == null || string.IsNullOrWhiteSpace(text)) return false;
        text = text.Trim();
        if (text.Length > MaxMessageLength) text = text.Substring(0, MaxMessageLength);

        if (WebSocketClient.Instance == null || !WebSocketClient.Instance.IsConnected)
        {
            AddSystem(Focus, "Нет соединения с сервером");
            return false;
        }

        var payload = new Dictionary<string, object> { { "type", MSG_CHAT }, { "text", text } };

        if (Focus.Type == MenuChatChannelType.Group)
        {
            if (!IsInGroup)
            {
                AddSystem(Focus, "Вы не состоите в группе");
                return false;
            }
            payload["channelType"] = "group";
        }
        else
        {
            payload["channelType"] = "private";
            payload["targetId"] = Focus.UserId;
        }

        Debug.LogError($"Chat -> {Focus.Type} '{Focus.Title}' (id {Focus.UserId}), {text.Length} симв.");

        WebSocketClient.Instance.SendMessageToServer(JsonConvert.SerializeObject(payload));
        return true;
    }

    private void OnMessageReceived(Dictionary<string, string> d, string json)
    {
        if (d == null || d.Count == 0) return;
        if (!d.TryGetValue("type", out string type)) return;

        switch (type)
        {
            case MSG_CHAT:
                Debug.LogError($"2 {json} type: {type}");
                HandleChatMessage(d);
                break;

            case MSG_CHAT_ERROR:
                if (_groupChannel != null)
                    AddSystem(_groupChannel, Get(d, "message"));
                break;

            case MSG_FRIEND_UI:
                if (_initialized && MyId >= 0)
                    RefreshFriendsAndConversations();
                break;

            case MSG_ONLINE_USERS:
                HandleOnlineSnapshot(d);
                break;

            case MSG_USER_ONLINE:
                HandleUserOnline(d);
                break;

            case MSG_USER_OFFLINE:
                HandleUserOffline(d);
                break;
        }
    }

    private void HandleChatMessage(Dictionary<string, string> d)
    {
        if (!_initialized)
        {
            Debug.LogError("Chat: сообщение пришло до Initialize(), проигнорировано");
            return;
        }

        string channelType = Get(d, "channelType");
        int.TryParse(Get(d, "senderId"), out int senderId);

        string rawLogin = Get(d, "senderLogin");
        string login = !string.IsNullOrEmpty(rawLogin) ? rawLogin
                     : senderId == MyId ? "Вы" : "Игрок #" + senderId;

        Debug.LogError($"Chat <- {channelType}, от {senderId}, канал '{Get(d, "channelId")}'");

        int channelsBefore = _channels.Count;
        MenuChatChannel ch;

        if (channelType == "group")
        {
            ch = _groupChannel;
        }
        else if (channelType == "private")
        {
            if (!int.TryParse(Get(d, "channelId"), out int otherId)) return;
            ch = GetOrCreatePrivate(otherId, senderId == otherId ? rawLogin : null);
        }
        else return;

        int.TryParse(Get(d, "messageId"), out int messageId);

        var msg = new MenuChatMessage
        {
            Id = messageId,
            SenderId = senderId,
            SenderLogin = login,
            Text = Get(d, "text"),
            Time = ParseTime(Get(d, "time")),
            IsMine = senderId == MyId
        };

        AddMessage(ch, msg);

        if (_channels.Count != channelsBefore)
            ChannelsChanged?.Invoke();
    }

    private void AddMessage(MenuChatChannel ch, MenuChatMessage msg)
    {
        if (msg.Id != 0 && ch.Messages.Any(m => m.Id == msg.Id)) return;

        ch.Messages.Add(msg);
        if (ch.Messages.Count > MaxCachedMessages)
            ch.Messages.RemoveAt(0);

        if (ch == Active)
        {
            MessageAdded?.Invoke(ch, msg);
        }
        else if (!msg.IsMine && !msg.IsSystem)
        {
            ch.Unread++;
            ChannelsChanged?.Invoke();
        }
    }

    private void AddSystem(MenuChatChannel ch, string text)
    {
        if (ch == null || string.IsNullOrEmpty(text)) return;
        AddMessage(ch, new MenuChatMessage { SenderId = -1, Text = text, Time = DateTime.UtcNow });
    }

    private void RefreshFriendsAndConversations()
    {
        var data = new Dictionary<string, string> { { "id", MyId.ToString() } };
        NetworkHTTP.Instance.Post(URLLibrary.GetFriendList, data,
            json =>
            {
                try
                {
                    var resp = JsonConvert.DeserializeObject<FriendListResponse>(json);
                    if (resp != null && resp.success && resp.friends != null)
                    {
                        _friendIds.Clear();
                        foreach (var f in resp.friends)
                        {
                            _friendIds.Add(f.id);
                            GetOrCreatePrivate(f.id, f.login);
                        }
                    }
                }
                catch (JsonException e) { Debug.LogError("Chat: friends parse error " + e.Message); }
                LoadConversations();
            });
    }

    private void LoadConversations()
    {
        var data = new Dictionary<string, string> { { "id", MyId.ToString() } };
        NetworkHTTP.Instance.Post(URLLibrary.GetChatConversations, data,
            json =>
            {
                try
                {
                    var resp = JsonConvert.DeserializeObject<ConversationsResponse>(json);
                    if (resp != null && resp.success && resp.conversations != null)
                        foreach (var c in resp.conversations)
                            GetOrCreatePrivate(c.id, c.login).HasConversation = true;
                }
                catch (JsonException e) { Debug.LogError("Chat: conversations parse error " + e.Message); }
                ChannelsChanged?.Invoke();
            });
    }

    private void LoadHistory(MenuChatChannel ch)
    {
        ch.HistoryRequested = true;
        var data = new Dictionary<string, string>
        {
            { "id", MyId.ToString() },
            { "other_id", ch.UserId.ToString() },
            { "limit", HistoryPageSize.ToString() }
        };
        NetworkHTTP.Instance.Post(URLLibrary.GetChatHistory, data,
            json => OnHistoryLoaded(ch, json),
            err => { ch.HistoryRequested = false; Debug.LogError("Chat: history error " + err); });
    }

    private void OnHistoryLoaded(MenuChatChannel ch, string json)
    {
        HistoryResponse resp;
        try { resp = JsonConvert.DeserializeObject<HistoryResponse>(json); }
        catch (JsonException e)
        {
            ch.HistoryRequested = false;
            Debug.LogError("Chat: history parse error " + e.Message);
            return;
        }
        if (resp == null || !resp.success || resp.messages == null)
        {
            ch.HistoryRequested = false;
            return;
        }

        var history = resp.messages.Select(m => new MenuChatMessage
        {
            Id = m.id,
            SenderId = m.sender_id,
            SenderLogin = m.sender_id == MyId ? "Вы" : ch.Title,
            Text = m.message,
            Time = ParseTime(m.created_at),
            IsMine = m.sender_id == MyId
        }).ToList();
        
        var ids = new HashSet<int>(history.Select(h => h.Id));

        var live = ch.Messages.Where(m => m.Id != 0
            ? !ids.Contains(m.Id)
            : !history.Any(h => h.SenderId == m.SenderId && h.Text == m.Text)).ToList();

        ch.Messages.Clear();
        ch.Messages.AddRange(history);
        ch.Messages.AddRange(live);
        ch.HistoryLoaded = true;

        if (Active == ch) HistoryLoaded?.Invoke(ch);
    }

    private static string Get(Dictionary<string, string> d, string k) => d.TryGetValue(k, out var v) ? v ?? "" : "";

    private static DateTime ParseTime(string s)
    {
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var t))
            return t;
        return DateTime.UtcNow;
    }

    public class FriendListResponse { public bool success; public List<UserDto> friends; }
    public class UserDto { public int id; public string login; }
    public class ConversationsResponse { public bool success; public List<UserDto> conversations; }
    public class HistoryResponse { public bool success; public List<HistoryItem> messages; }
    public class HistoryItem { public int id; public int sender_id; public int receiver_id; public string message; public string created_at; }
}