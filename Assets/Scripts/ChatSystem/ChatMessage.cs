public class ChatMessage
{
    public ChatChannel Channel { get; }
    public int SenderId { get; }
    public string SenderName { get; }
    public string HeroName { get; }
    public int SenderTeam { get; }
    public int RecipientId { get; }
    public string RecipientName { get; }
    public long UnixTime { get; }
    public string Text { get; }
    public bool IsOwn { get; }

    public int PeerId => IsOwn ? RecipientId : SenderId;
    public string PeerName => IsOwn ? RecipientName : SenderName;

    public ChatMessage(in ChatPacket packet, int localUserId)
    {
        Channel = packet.Channel;
        SenderId = packet.SenderId;
        SenderName = packet.SenderName;
        HeroName = packet.HeroName;
        SenderTeam = packet.SenderTeam;
        RecipientId = packet.RecipientId;
        RecipientName = packet.RecipientName;
        UnixTime = packet.UnixTime;
        Text = packet.Text;
        IsOwn = packet.SenderId == localUserId;
    }
}