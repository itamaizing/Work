using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public partial class MenuChatService
{
    private const string MSG_CH_CREATE = "createChannel";
    private const string MSG_CH_LIST_REQ = "listChannels";
    private const string MSG_CH_JOIN = "joinChannel";
    private const string MSG_CH_LEAVE = "leaveChannel";
    private const string MSG_CH_DELETE = "deleteChannel";
    private const string MSG_CH_KICK = "kickFromChannel";

    private const string MSG_CH_INFO = "channelInfo";
    private const string MSG_CH_LIST = "channelList";
    private const string MSG_CH_MEMBER_JOINED = "channelMemberJoined";
    private const string MSG_CH_MEMBER_LEFT = "channelMemberLeft";
    private const string MSG_CH_LEFT = "channelLeft";
    private const string MSG_CH_KICKED = "channelKicked";
    private const string MSG_CH_DELETED = "channelDeleted";
    private const string MSG_CH_ERROR = "channelError";
    private const string MSG_ERROR = "error";

    public const int ChannelNameMaxLength = 32;
    
    private const float ChannelErrorWindow = 5f;

    public class ChannelListItem
    {
        public string channelId;
        public string name;
        public string ownerId;
        public string members;
        public string capacity;
        public string joined;
        public string kind;
    }

    private readonly Dictionary<string, ChannelListItem> _channelDirectory = new Dictionary<string, ChannelListItem>();
    private readonly List<string> _rejoinIds = new List<string>();
    private string _focusChannelId;
    private string _focusCreatedName;
    private string _pendingJoinName;
    private bool _listForDisplay;
    private float _lastChannelRequestAt = -100f;

    public bool IsChannelOwner(MenuChatChannel c) =>
        c != null && c.Type == MenuChatChannelType.Custom && c.OwnerId == MyId;

    #region Запросы

    public void CreateChannel(string name)
    {
        name = (name ?? string.Empty).Trim();
        if (name.Length > ChannelNameMaxLength) name = name.Substring(0, ChannelNameMaxLength);
        if (name.Length == 0)
        {
            AddSystem(Active, "Использование: /create <название>");
            return;
        }

        _focusCreatedName = name;
        SendChannelRequest(new Dictionary<string, object> { { "type", MSG_CH_CREATE }, { "name", name } });
    }

    public void RequestChannelList(bool display)
    {
        _listForDisplay |= display;
        SendChannelRequest(new Dictionary<string, object> { { "type", MSG_CH_LIST_REQ } });
    }
    
    public void JoinChannelById(string channelId, bool focus)
    {
        if (focus) _focusChannelId = channelId;
        SendChannelRequest(new Dictionary<string, object> { { "type", MSG_CH_JOIN }, { "channelId", channelId } });
    }

    public void JoinChannelByName(string name)
    {
        var joined = FindJoinedChannel(name);
        if (joined != null)
        {
            SetFocus(joined);
            return;
        }

        var entry = _channelDirectory.Values.FirstOrDefault(e => string.Equals(e.name, name, StringComparison.OrdinalIgnoreCase));
        if (entry != null)
        {
            JoinChannelById(entry.channelId, true);
            return;
        }

        _pendingJoinName = name;
        RequestChannelList(false);
    }

    public void LeaveChannel(MenuChatChannel ch)
    {
        SendChannelRequest(new Dictionary<string, object> { { "type", MSG_CH_LEAVE }, { "channelId", ch.ChannelId } });
    }

    public void DeleteChannel(MenuChatChannel ch)
    {
        SendChannelRequest(new Dictionary<string, object> { { "type", MSG_CH_DELETE }, { "channelId", ch.ChannelId } });
    }

    public void KickFromChannel(MenuChatChannel ch, int targetId)
    {
        SendChannelRequest(new Dictionary<string, object>
        {
            { "type", MSG_CH_KICK }, { "channelId", ch.ChannelId }, { "targetId", targetId }
        });
    }

    private bool SendChannelRequest(Dictionary<string, object> payload)
    {
        if (WebSocketClient.Instance == null || !WebSocketClient.Instance.IsConnected)
        {
            AddSystem(Active, "Нет соединения с сервером");
            return false;
        }

        _lastChannelRequestAt = Time.unscaledTime;
        WebSocketClient.Instance.SendMessageToServer(JsonConvert.SerializeObject(payload));
        return true;
    }

    #endregion

    #region Поиск каналов

    public MenuChatChannel FindJoinedChannel(string name)
    {
        return _channels.Values.FirstOrDefault(c =>
            c.Type == MenuChatChannelType.Custom && c.Joined
            && string.Equals(c.Title, name, StringComparison.OrdinalIgnoreCase));
    }

    private MenuChatChannel GetOrCreateCustom(string channelId, string name)
    {
        string key = MenuChatChannel.CustomKey(channelId);
        if (!_channels.TryGetValue(key, out var ch))
        {
            ch = new MenuChatChannel(channelId, string.IsNullOrEmpty(name) ? "Канал" : name);
            _channels[key] = ch;
        }
        else if (!string.IsNullOrEmpty(name))
        {
            ch.Title = name;
        }
        return ch;
    }

    private MenuChatChannel FindCustomById(string channelId) =>
        _channels.TryGetValue(MenuChatChannel.CustomKey(channelId), out var ch) ? ch : null;
    
    private MenuChatChannel ResolveChannelArg(string name)
    {
        if (!string.IsNullOrWhiteSpace(name))
            return FindJoinedChannel(name.Trim());

        return Focus != null && Focus.Type == MenuChatChannelType.Custom ? Focus : null;
    }

    private void SplitChannelArgs(string args, out string name, out string message, out bool known)
    {
        args = args.Trim();

        string best = null;
        foreach (string candidate in KnownChannelNames())
        {
            if (candidate.Length == 0 || !args.StartsWith(candidate, StringComparison.OrdinalIgnoreCase)) continue;
            if (args.Length > candidate.Length && !char.IsWhiteSpace(args[candidate.Length])) continue;
            if (best == null || candidate.Length > best.Length) best = candidate;
        }

        if (best != null)
        {
            name = best;
            message = args.Substring(best.Length).Trim();
            known = true;
            return;
        }

        int space = args.IndexOf(' ');
        name = space < 0 ? args : args.Substring(0, space);
        message = space < 0 ? string.Empty : args.Substring(space + 1).Trim();
        known = false;
    }

    private IEnumerable<string> KnownChannelNames()
    {
        foreach (var c in _channels.Values)
            if (c.Type == MenuChatChannelType.Custom && c.Joined) yield return c.Title;
        foreach (var e in _channelDirectory.Values)
            if (!string.IsNullOrEmpty(e.name)) yield return e.name;
    }

    #endregion

    #region Команды

    private void CmdJoin(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            AddSystem(Active, "Использование: /join <канал>  или  /channel <канал> <сообщение>");
            return;
        }

        SplitChannelArgs(args, out string name, out string message, out bool known);
        
        if (!known)
        {
            name = args.Trim();
            message = string.Empty;
        }

        if (message.Length == 0)
        {
            JoinChannelByName(name);
            return;
        }

        var ch = FindJoinedChannel(name);
        if (ch == null)
        {
            AddSystem(Active, $"Вы не состоите в канале «{name}». Войти: /join {name}");
            return;
        }

        SendTo(ch, message);
    }

    private void CmdCreate(string args) => CreateChannel(args);

    private void CmdLeave(string args)
    {
        var ch = ResolveChannelArg(args);
        if (ch == null)
        {
            AddSystem(Active, "Использование: /leave <канал>  (без названия - канал, на который сейчас фокус)");
            return;
        }
        if (ch.IsSystemChannel)
        {
            AddSystem(Active, $"Из канала «{ch.Title}» выйти нельзя");
            return;
        }

        LeaveChannel(ch);
    }

    private void CmdDelete(string args)
    {
        var ch = ResolveChannelArg(args);
        if (ch == null)
        {
            AddSystem(Active, "Использование: /delete <канал>");
            return;
        }
        if (!IsChannelOwner(ch))
        {
            AddSystem(Active, "Только хост может удалить канал");
            return;
        }

        DeleteChannel(ch);
    }

    private void CmdKick(string args)
    {
        args = args.Trim();
        int space = args.IndexOf(' ');
        string nick = space < 0 ? args : args.Substring(0, space);
        string channelName = space < 0 ? string.Empty : args.Substring(space + 1).Trim();

        if (nick.Length == 0)
        {
            AddSystem(Active, "Использование: /kick <ник> [канал]");
            return;
        }

        var ch = ResolveChannelArg(channelName);
        if (ch == null)
        {
            AddSystem(Active, "Укажите канал: /kick <ник> <канал>");
            return;
        }
        if (!IsChannelOwner(ch))
        {
            AddSystem(Active, "Исключать игроков может только хост канала");
            return;
        }

        var member = ch.Members.FirstOrDefault(m => string.Equals(m.Value, nick, StringComparison.OrdinalIgnoreCase));
        if (member.Value == null)
        {
            AddSystem(Active, $"Игрока «{nick}» нет в канале «{ch.Title}»");
            return;
        }
        if (member.Key == MyId)
        {
            AddSystem(Active, "Нельзя исключить самого себя. Выйти: /leave");
            return;
        }

        KickFromChannel(ch, member.Key);
    }

    private void CmdMembers(string args)
    {
        var ch = ResolveChannelArg(args);
        if (ch == null)
        {
            AddSystem(Active, "Использование: /members <канал>");
            return;
        }

        string list = string.Join(", ", ch.Members
            .OrderBy(m => m.Value, StringComparer.CurrentCultureIgnoreCase)
            .Select(m => m.Key == ch.OwnerId ? m.Value + " ★" : m.Value));

        AddSystem(Active, $"«{ch.Title}» ({ch.Members.Count}/{ch.Capacity}): {list}");
    }

    #endregion

    #region Ответы сервера

    private void HandleChannelMessage(string type, Dictionary<string, string> d)
    {
        switch (type)
        {
            case MSG_CH_INFO: HandleChannelInfo(d); break;
            case MSG_CH_LIST: HandleChannelList(d); break;
            case MSG_CH_MEMBER_JOINED: HandleMemberJoined(d); break;
            case MSG_CH_MEMBER_LEFT: HandleMemberLeft(d); break;

            case MSG_CH_LEFT:
                RemoveCustomChannel(FindCustomById(Get(d, "channelId")), "Вы вышли из канала «{0}»");
                break;

            case MSG_CH_KICKED:
                RemoveCustomChannel(FindCustomById(Get(d, "channelId")), "Вас исключили из канала «{0}»");
                break;

            case MSG_CH_DELETED:
                RemoveCustomChannel(FindCustomById(Get(d, "channelId")),
                    Get(d, "reason") == "owner_left"
                        ? "Хост покинул канал «{0}», канал закрыт"
                        : "Канал «{0}» удалён");
                break;

            case MSG_CH_ERROR:
                AddSystem(Active, Get(d, "message"));
                break;

            case MSG_ERROR:
                if (Time.unscaledTime - _lastChannelRequestAt < ChannelErrorWindow)
                    AddSystem(Active, Get(d, "message"));
                break;
        }
    }

    private void HandleChannelInfo(Dictionary<string, string> d)
    {
        string id = Get(d, "channelId");
        if (string.IsNullOrEmpty(id)) return;

        var ch = GetOrCreateCustom(id, Get(d, "name"));
        int.TryParse(Get(d, "ownerId"), out int owner);
        int.TryParse(Get(d, "capacity"), out int capacity);
        ch.OwnerId = owner;
        ch.Capacity = capacity;
        ch.Kind = Get(d, "kind");

        ch.Members.Clear();
        try
        {
            var members = JsonConvert.DeserializeObject<List<UserDto>>(Get(d, "members"));
            if (members != null)
                foreach (var m in members)
                    ch.Members[m.id] = string.IsNullOrEmpty(m.login) ? "Игрок #" + m.id : m.login;
        }
        catch (JsonException e) { Debug.LogError("Chat: channel members parse error " + e.Message); }

        bool firstTime = !ch.Joined;
        ch.Joined = true;
        _channelDirectory.Remove(id);

        if (firstTime)
            AddSystem(ch, $"Вы в канале «{ch.Title}» ({ch.Members.Count}/{ch.Capacity}). Выйти: /leave");

        bool focus = _focusChannelId == id
            || (!string.IsNullOrEmpty(_focusCreatedName) && owner == MyId
                && string.Equals(_focusCreatedName, ch.Title, StringComparison.OrdinalIgnoreCase));
        if (focus)
        {
            _focusChannelId = null;
            _focusCreatedName = null;
        }

        ChannelsChanged?.Invoke();
        if (focus) SetFocus(ch);
    }

    private void HandleChannelList(Dictionary<string, string> d)
    {
        List<ChannelListItem> items = null;
        try { items = JsonConvert.DeserializeObject<List<ChannelListItem>>(Get(d, "channels")); }
        catch (JsonException e) { Debug.LogError("Chat: channel list parse error " + e.Message); }
        if (items == null) items = new List<ChannelListItem>();

        _channelDirectory.Clear();
        foreach (var item in items)
            if (!string.IsNullOrEmpty(item.channelId)) _channelDirectory[item.channelId] = item;
        
        if (_rejoinIds.Count > 0)
        {
            foreach (string id in _rejoinIds.ToList())
            {
                var ch = FindCustomById(id);
                if (ch == null) continue;

                if (_channelDirectory.ContainsKey(id)) JoinChannelById(id, false);
                else RemoveCustomChannel(ch, "Канал «{0}» закрыт, пока вы были не в сети");
            }
            _rejoinIds.Clear();
        }

        if (_pendingJoinName != null)
        {
            string name = _pendingJoinName;
            _pendingJoinName = null;

            var entry = items.FirstOrDefault(e => string.Equals(e.name, name, StringComparison.OrdinalIgnoreCase));
            if (entry != null) JoinChannelById(entry.channelId, true);
            else AddSystem(Active, $"Канал «{name}» не найден. Список каналов: /channels");
        }

        if (_listForDisplay)
        {
            _listForDisplay = false;
            ShowChannelList(items);
        }
    }

    private void ShowChannelList(List<ChannelListItem> items)
    {
        if (items.Count == 0)
        {
            AddSystem(Active, "Открытых каналов нет. Создать свой: /create <название>");
            return;
        }

        var lines = items
            .OrderBy(i => i.name, StringComparer.CurrentCultureIgnoreCase)
            .Select(i => $"{i.name} ({i.members}/{i.capacity}){(i.joined == "1" ? " - вы в канале" : string.Empty)}");

        AddSystem(Active, "Каналы:\n" + string.Join("\n", lines) + "\nВойти: /join <название>");
    }

    private void HandleMemberJoined(Dictionary<string, string> d)
    {
        var ch = FindCustomById(Get(d, "channelId"));
        if (ch == null || !int.TryParse(Get(d, "playerId"), out int id)) return;

        string login = Get(d, "login");
        if (string.IsNullOrEmpty(login)) login = "Игрок #" + id;

        ch.Members[id] = login;
        if (id != MyId) AddSystem(ch, $"{login} вошёл в канал");
        ChannelsChanged?.Invoke();
    }

    private void HandleMemberLeft(Dictionary<string, string> d)
    {
        var ch = FindCustomById(Get(d, "channelId"));
        if (ch == null || !int.TryParse(Get(d, "playerId"), out int id)) return;

        ch.Members.TryGetValue(id, out string login);
        if (string.IsNullOrEmpty(login)) login = "Игрок #" + id;
        ch.Members.Remove(id);

        AddSystem(ch, Get(d, "kicked") == "1" ? $"{login} исключён из канала" : $"{login} вышел из канала");
        ChannelsChanged?.Invoke();
    }
    
    private void RemoveCustomChannel(MenuChatChannel ch, string noticeFormat)
    {
        if (ch == null) return;

        _channels.Remove(ch.Key);
        _rejoinIds.Remove(ch.ChannelId);

        bool wasActive = Active == ch;
        bool wasFocus = Focus == ch;
        if (wasActive) Active = _groupChannel;
        if (wasFocus) Focus = _groupChannel;

        ChannelsChanged?.Invoke();
        if (wasActive) ActiveChanged?.Invoke(Active);
        if (wasFocus && Focus != null) FocusChanged?.Invoke(Focus);

        if (!string.IsNullOrEmpty(noticeFormat))
            AddSystem(Active, string.Format(noticeFormat, ch.Title));
    }

    #endregion

    #region Соединение

    private void ChannelsOnSocketDisconnected()
    {
        _rejoinIds.Clear();
        foreach (var c in _channels.Values)
            if (c.Type == MenuChatChannelType.Custom && c.Joined && !c.IsSystemChannel)
                _rejoinIds.Add(c.ChannelId);
    }

    private void ChannelsOnSocketReconnected()
    {
        if (_rejoinIds.Count > 0)
            RequestChannelList(false);
    }

    private void ResetChannelState()
    {
        _channelDirectory.Clear();
        _rejoinIds.Clear();
        _focusChannelId = null;
        _focusCreatedName = null;
        _pendingJoinName = null;
        _listForDisplay = false;
    }

    #endregion
}