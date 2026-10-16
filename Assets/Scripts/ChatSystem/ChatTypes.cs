using System;

public enum ChatChannel : byte
{
    All,
    Team,
    Group,
    Private
}

public enum ChatState
{
    Hidden,
    Unfocused,
    Focused
}

public readonly struct ChatTarget : IEquatable<ChatTarget>
{
    public readonly ChatChannel Channel;
    public readonly int PeerId;
    public readonly string PeerName;

    public ChatTarget(ChatChannel channel, int peerId = 0, string peerName = null)
    {
        Channel = channel;
        PeerId = peerId;
        PeerName = peerName;
    }

    public static ChatTarget Of(ChatChannel channel) => new ChatTarget(channel);
    public static ChatTarget ForPeer(int peerId, string peerName) => new ChatTarget(ChatChannel.Private, peerId, peerName);
    
    public string Label => Channel == ChatChannel.Private
        ? (PeerName ?? "?")
        : G.Colors.Chat.GetStyle(Channel).Label;

    public bool Equals(ChatTarget other) => Channel == other.Channel && PeerId == other.PeerId;
    public override bool Equals(object obj) => obj is ChatTarget other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Channel, PeerId);
}

public struct ChatPacket
{
    public ChatChannel Channel;
    public int SenderId;
    public string SenderName;
    public string HeroName;
    public int SenderTeam;
    public int RecipientId;
    public string RecipientName;
    public long UnixTime;
    public string Text;
}

public struct ChatPlayerInfo
{
    public int UserId;
    public string Name;
    public string HeroName;
    public int TeamIndex;
    public int[] GroupMembers;
}