using System;
using System.Collections.Generic;

public enum MenuChatChannelType { Private, Group, Custom }

public enum MenuChatGroup { Users, Friends, Channels }

public enum MenuChatMessageState { Sent, Pending, Failed }

public class MenuChatMessage
{
    public int Id;
    public string ClientId;
    public int SenderId;
    public string SenderLogin;
    public string Text;
    public DateTime Time;
    public bool IsMine;

    public MenuChatMessageState State = MenuChatMessageState.Sent;
    public bool Delivered = true;
    public string Error;

    public bool IsSystem => SenderId < 0;
}

public class MenuChatChannel
{
    public const string GroupKey = "group";

    public readonly MenuChatChannelType Type;
    public readonly int UserId;
    public string Title;
    public readonly List<MenuChatMessage> Messages = new List<MenuChatMessage>();
    public int Unread;
    public bool HistoryRequested;
    public bool HistoryLoaded;
    public bool HasConversation;

    public readonly string ChannelId;
    public int OwnerId;
    public int Capacity;
    public bool Joined;
    public string Kind = string.Empty;
    public readonly Dictionary<int, string> Members = new Dictionary<int, string>();

    public bool IsSystemChannel => !string.IsNullOrEmpty(Kind) && Kind != "user";

    public string Key
    {
        get
        {
            switch (Type)
            {
                case MenuChatChannelType.Group: return GroupKey;
                case MenuChatChannelType.Custom: return CustomKey(ChannelId);
                default: return PrivateKey(UserId);
            }
        }
    }

    public static string PrivateKey(int userId) => "u:" + userId;
    public static string CustomKey(string channelId) => "c:" + channelId;

    public MenuChatChannel(MenuChatChannelType type, int userId, string title)
    {
        Type = type;
        UserId = userId;
        Title = title;
    }

    public MenuChatChannel(string channelId, string title)
        : this(MenuChatChannelType.Custom, 0, title)
    {
        ChannelId = channelId;
    }
}