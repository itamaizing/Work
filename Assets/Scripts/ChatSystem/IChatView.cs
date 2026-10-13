using System;
using System.Collections.Generic;

public interface IChatView
{
    event Action<string> OnMessageSent;
    event Action<int> OnChannelSelected;

    void SetState(ChatState state);
    void AddMessage(ChatMessage message);
    void ClearMessages();
    void SetChannels(IReadOnlyList<ChatTarget> targets, int activeIndex);
    void SetActiveChannel(int index);
    void ScrollToBottom();
}