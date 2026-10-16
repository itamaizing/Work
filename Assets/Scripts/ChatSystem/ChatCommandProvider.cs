using System;
using System.Collections.Generic;
using System.Text;

public readonly struct ChatPeer
{
    public readonly int Id;
    public readonly string Name;

    public ChatPeer(int id, string name)
    {
        Id = id;
        Name = name;
    }
}

public abstract class ChatCommandProvider
{
    private sealed class Command
    {
        public string[] Names;
        public string Usage;
        public string Description;
        public Action<string> Handler;
        public bool MatchOnly;
    }

    private readonly List<Command> _commands = new List<Command>();
    private readonly Dictionary<string, Command> _byName = new Dictionary<string, Command>(StringComparer.OrdinalIgnoreCase);
    private bool _registered;
    private ChatPeer? _lastWhisper;
    protected abstract int LocalUserId { get; }
    protected abstract bool HasGroup { get; }
    protected abstract void Say(string text);
    protected abstract void Resolve(string nick, bool requireId, Action<ChatPeer> found, Action<string> failed);

    protected abstract void FocusPrivate(ChatPeer peer);
    protected abstract void SendPrivate(ChatPeer peer, string text);
    protected abstract void FocusGroup();
    protected abstract void SendGroup(string text);
    protected virtual bool IsInMatch => false;
    protected virtual void FocusAll() { }
    protected virtual void FocusTeam() { }

    #region API
    
    public bool TryExecute(string text)
    {
        if (string.IsNullOrEmpty(text) || text[0] != '/') return false;

        string body = text.Substring(1);
        int space = IndexOfWhitespace(body);
        string word = space < 0 ? body : body.Substring(0, space);
        if (word.Length == 0) return false;

        string args = space < 0 ? string.Empty : body.Substring(space + 1).Trim();

        EnsureRegistered();

        if (!_byName.TryGetValue(word, out Command command))
        {
            Say($"Неизвестная команда «/{word}». Список команд: /help");
            return true;
        }

        if (command.MatchOnly && !IsInMatch)
        {
            Say($"Команда «/{word}» доступна только внутри матча");
            return true;
        }

        command.Handler(args);
        return true;
    }
    
    public void NoteIncomingPrivate(int userId, string name)
    {
        if (userId == LocalUserId) return;
        _lastWhisper = new ChatPeer(userId, string.IsNullOrEmpty(name) ? "Игрок #" + userId : name);
    }

    public void ResetSession() => _lastWhisper = null;

    #endregion

    #region Регистрация Команд
    
    protected virtual void RegisterCommands()
    {
        Register(new[] { "help", "помощь" }, "/help", "список команд", CmdHelp);

        Register(new[] { "w", "whisper", "m", "msg", "message", "ц", "л" },
            "/w <ник> [сообщение]",
            "без текста - писать игроку в личку, с текстом - отправить сообщение", CmdWhisper);

        Register(new[] { "r" }, "/r [сообщение]",
            "ответить тому, кто писал в личку последним", CmdReply);

        Register(new[] { "group", "группа", "g", "г" }, "/group [сообщение]",
            "писать в чат группы / отправить в чат группы", CmdGroup);

        Register(new[] { "all", "общий" }, "/all", "писать в общий чат", _ => FocusAll(), matchOnly: true);
        Register(new[] { "team", "союзный" }, "/team", "писать в союзный чат", _ => FocusTeam(), matchOnly: true);

        Register(new[] { "block", "блок", "чс", "ignore" }, "/block <ник>",
            "добавить игрока в чёрный список", CmdBlock);

        Register(new[] { "unblock", "разблок", "unignore" }, "/unblock <ник>",
            "убрать игрока из чёрного списка", CmdUnblock);
    }

    protected void Register(string[] names, string usage, string description, Action<string> handler, bool matchOnly = false)
    {
        var command = new Command
        {
            Names = names,
            Usage = usage,
            Description = description,
            Handler = handler,
            MatchOnly = matchOnly
        };

        _commands.Add(command);
        foreach (string name in names)
            _byName[name] = command;
    }

    private void EnsureRegistered()
    {
        if (_registered) return;
        _registered = true;
        RegisterCommands();
    }

    #endregion

    #region Команды

    private void CmdHelp(string args)
    {
        var sb = new StringBuilder("Команды:");

        foreach (Command command in _commands)
        {
            if (command.MatchOnly && !IsInMatch) continue;

            sb.Append('\n').Append(command.Usage).Append(" - ").Append(command.Description);

            if (command.Names.Length > 1)
            {
                sb.Append(" (также: ");
                for (int i = 1; i < command.Names.Length; i++)
                {
                    if (i > 1) sb.Append(", ");
                    sb.Append('/').Append(command.Names[i]);
                }
                sb.Append(')');
            }
        }

        sb.Append("\nОформление в сообщениях: <b> <i> <u> <s> <sub> <sup> <color=#RRGGBB>");

        Say(sb.ToString());
    }

    private void CmdWhisper(string args)
    {
        SplitFirst(args, out string nick, out string message);
        if (nick.Length == 0)
        {
            Say("Использование: /w <ник> [сообщение]");
            return;
        }

        Resolve(nick, false, peer =>
        {
            if (peer.Id != 0 && peer.Id == LocalUserId)
            {
                Say("Нельзя написать самому себе");
                return;
            }

            if (message.Length == 0) FocusPrivate(peer);
            else SendPrivate(peer, message);
        }, Say);
    }

    private void CmdReply(string args)
    {
        if (_lastWhisper == null)
        {
            Say("Вам ещё никто не писал в личные сообщения");
            return;
        }

        ChatPeer peer = _lastWhisper.Value;
        if (args.Length == 0) FocusPrivate(peer);
        else SendPrivate(peer, args);
    }

    private void CmdGroup(string args)
    {
        if (!HasGroup)
        {
            Say("Вы не состоите в группе");
            return;
        }

        if (args.Length == 0) FocusGroup();
        else SendGroup(args);
    }

    private void CmdBlock(string args)
    {
        SplitFirst(args, out string nick, out _);
        if (nick.Length == 0)
        {
            Say("Использование: /block <ник>");
            return;
        }

        Resolve(nick, true, peer =>
        {
            if (peer.Id <= 0)
            {
                Say($"Не удалось определить игрока «{nick}»");
                return;
            }

            if (peer.Id == LocalUserId)
            {
                Say("Нельзя добавить в чёрный список самого себя");
                return;
            }

            Say(ChatBlockList.Add(peer.Id, peer.Name)
                ? $"Игрок «{peer.Name}» добавлен в чёрный список"
                : $"Игрок «{peer.Name}» уже в чёрном списке");
        }, Say);
    }

    private void CmdUnblock(string args)
    {
        SplitFirst(args, out string nick, out _);
        if (nick.Length == 0)
        {
            Say("Использование: /unblock <ник>");
            return;
        }

        if (ChatBlockList.TryFindByName(nick, out int id) && ChatBlockList.Remove(id))
            Say($"Игрок «{nick}» убран из чёрного списка");
        else
            Say($"Игрока «{nick}» нет в чёрном списке");
    }

    #endregion

    #region Утилиты

    private static void SplitFirst(string args, out string first, out string rest)
    {
        args = args.Trim();
        int space = IndexOfWhitespace(args);
        first = space < 0 ? args : args.Substring(0, space);
        rest = space < 0 ? string.Empty : args.Substring(space + 1).Trim();
    }

    private static int IndexOfWhitespace(string s)
    {
        for (int i = 0; i < s.Length; i++)
            if (char.IsWhiteSpace(s[i])) return i;
        return -1;
    }

    #endregion
}