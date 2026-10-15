using System;
using System.Collections.Generic;

public enum MenuChatChannelType { Private, Group }

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

    public string Key => Type == MenuChatChannelType.Group ? GroupKey : PrivateKey(UserId);
    public static string PrivateKey(int userId) => "u:" + userId;

    public MenuChatChannel(MenuChatChannelType type, int userId, string title)
    {
        Type = type;
        UserId = userId;
        Title = title;
    }
}