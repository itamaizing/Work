using System;

public interface IChatView
{
    event Action<string, ChatChannel> OnMessageSent;
    
    void SetState(ChatState state);
    void AddMessage(ChatMessage message);
    void ClearMessages();
    void SetCurrentChannel(ChatChannel channel);
    void ScrollToBottom();
}
