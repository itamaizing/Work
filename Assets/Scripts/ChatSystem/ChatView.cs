using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatView : MonoBehaviour, IChatView
{
    [Header("UI References")]
    [SerializeField] private GameObject _focusedPanel;
    [SerializeField] private GameObject _unfocusedPanel;
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private Transform _messageContainer;
    [SerializeField] private Transform _unfocusedMessageContainer;
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private TMP_Dropdown _channelDropdown;
    [SerializeField] private ChatMessageUI _messagePrefab;

    [Header("Settings")]
    [SerializeField] private ChatConfig _config;

    public event Action<string> OnMessageSent;
    public event Action<int> OnChannelSelected;

    private readonly List<string> _history = new();
    private readonly List<ChatMessageUI> _focusedUi = new();
    private readonly List<ChatMessageUI> _unfocusedUi = new();

    private void Awake()
    {
        if (_inputField != null)
        {
            _inputField.characterLimit = _config.MaxMessageLength;
            _inputField.lineType = TMP_InputField.LineType.SingleLine;
            _inputField.onSubmit.AddListener(OnSubmit);
        }

        if (_channelDropdown != null)
            _channelDropdown.onValueChanged.AddListener(OnDropdownChanged);

        SetState(ChatState.Hidden);
    }

    public void SetState(ChatState state)
    {
        switch (state)
        {
            case ChatState.Hidden:
                _focusedPanel.SetActive(false);
                _unfocusedPanel.SetActive(false);
                break;

            case ChatState.Unfocused:
                _focusedPanel.SetActive(false);
                _unfocusedPanel.SetActive(true);
                UpdateUnfocusedMessages();
                break;

            case ChatState.Focused:
                _focusedPanel.SetActive(true);
                _unfocusedPanel.SetActive(false);
                _inputField.ActivateInputField();
                _inputField.Select();
                break;
        }
    }

    public void AddMessage(ChatMessage message)
    {
        if (_messagePrefab == null || _messageContainer == null) return;

        string text = ChatMessageFormatter.Format(message);
        _history.Add(text);

        var ui = Instantiate(_messagePrefab, _messageContainer);
        ui.SetText(text);
        _focusedUi.Add(ui);

        while (_history.Count > _config.MaxHistory)
        {
            _history.RemoveAt(0);
            Destroy(_focusedUi[0].gameObject);
            _focusedUi.RemoveAt(0);
        }

        UpdateUnfocusedMessages();
    }

    public void ClearMessages()
    {
        _history.Clear();

        foreach (var ui in _focusedUi)
            if (ui != null) Destroy(ui.gameObject);
        _focusedUi.Clear();

        foreach (var ui in _unfocusedUi)
            if (ui != null) Destroy(ui.gameObject);
        _unfocusedUi.Clear();
    }

    public void SetChannels(IReadOnlyList<ChatTarget> targets, int activeIndex)
    {
        if (_channelDropdown == null) return;

        var options = new List<TMP_Dropdown.OptionData>(targets.Count);
        foreach (var target in targets)
            options.Add(new TMP_Dropdown.OptionData(target.Label));

        _channelDropdown.options = options;
        SetActiveChannel(activeIndex);
    }

    public void SetActiveChannel(int index)
    {
        if (_channelDropdown == null) return;

        _channelDropdown.SetValueWithoutNotify(index);
        _channelDropdown.RefreshShownValue();
    }

    public void ScrollToBottom()
    {
        if (_scrollRect == null) return;

        Canvas.ForceUpdateCanvases();
        _scrollRect.verticalNormalizedPosition = 0f;
    }

    private void UpdateUnfocusedMessages()
    {
        if (_unfocusedMessageContainer == null || _messagePrefab == null) return;

        int count = Mathf.Min(_history.Count, _config.MaxUnfocusedMessages);

        while (_unfocusedUi.Count < count)
            _unfocusedUi.Add(Instantiate(_messagePrefab, _unfocusedMessageContainer));

        while (_unfocusedUi.Count > count)
        {
            Destroy(_unfocusedUi[_unfocusedUi.Count - 1].gameObject);
            _unfocusedUi.RemoveAt(_unfocusedUi.Count - 1);
        }

        int start = _history.Count - count;
        for (int i = 0; i < count; i++)
            _unfocusedUi[i].SetText(_history[start + i]);
    }

    private void OnSubmit(string text)
    {
        OnMessageSent?.Invoke(text);
        _inputField.text = string.Empty;
    }

    private void OnDropdownChanged(int index)
    {
        OnChannelSelected?.Invoke(index);

        if (_inputField != null)
            _inputField.ActivateInputField();
    }

    private void OnDestroy()
    {
        if (_inputField != null)
            _inputField.onSubmit.RemoveListener(OnSubmit);

        if (_channelDropdown != null)
            _channelDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
    }
}