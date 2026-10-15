using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuChatUI : MonoBehaviour
{
    [Serializable]
    private class GroupView
    {
        public MenuChatGroup Group;
        public string Title;
        public GameObject Root;
        public Button Header;
        public TMP_Text HeaderLabel;
        public RectTransform Container;
        [NonSerialized] public bool Collapsed;
    }

    [Header("History")]
    [SerializeField] private ScrollRect _historyScroll;
    [SerializeField] private RectTransform _historyContent;
    [SerializeField] private TMP_Text _messagePrefab;

    [Header("Input")]
    [SerializeField] private TMP_InputField _input;
    [SerializeField] private Button _sendButton;
    [SerializeField] private TMP_Dropdown _focusDropdown;

    [Header("Channels panel")]
    [SerializeField] private List<GroupView> _groups;
    [SerializeField] private Button _channelButtonPrefab;

    private MenuChatService _service;
    private List<MenuChatChannel> _ordered = new List<MenuChatChannel>();
    private Coroutine _bindRoutine;
    private bool _bound;

    private void OnEnable()
    {
        _bindRoutine = StartCoroutine(Bind());
    }

    private void OnDisable()
    {
        if (_bindRoutine != null) StopCoroutine(_bindRoutine);
        Unbind();
    }

    private IEnumerator Bind()
    {
        while (MenuChatService.Instance == null)
            yield return null;

        _service = MenuChatService.Instance;

        _service.ChannelsChanged += RebuildChannels;
        _service.ActiveChanged += OnActiveChanged;
        _service.FocusChanged += OnFocusChanged;
        _service.MessageAdded += OnMessageAdded;
        _service.MessageUpdated += OnMessageUpdated;
        _service.HistoryLoaded += OnHistoryLoaded;

        _sendButton.onClick.AddListener(OnSend);
        _input.onSubmit.AddListener(OnSubmit);
        _input.characterLimit = MenuChatService.MaxMessageLength;
        _focusDropdown.onValueChanged.AddListener(OnFocusDropdown);

        foreach (var g in _groups)
        {
            var view = g;
            view.Header.onClick.AddListener(() => ToggleGroup(view));
        }

        _bound = true;
        _service.Initialize();
        RebuildChannels();
        RebuildHistory();
    }

    private void Unbind()
    {
        if (!_bound) return;
        _bound = false;

        _service.ChannelsChanged -= RebuildChannels;
        _service.ActiveChanged -= OnActiveChanged;
        _service.FocusChanged -= OnFocusChanged;
        _service.MessageAdded -= OnMessageAdded;
        _service.MessageUpdated -= OnMessageUpdated;
        _service.HistoryLoaded -= OnHistoryLoaded;

        _sendButton.onClick.RemoveListener(OnSend);
        _input.onSubmit.RemoveListener(OnSubmit);
        _focusDropdown.onValueChanged.RemoveListener(OnFocusDropdown);
        foreach (var g in _groups) g.Header.onClick.RemoveAllListeners();
    }

    private void OnSubmit(string _) => OnSend();

    private void OnSend()
    {
        if (!string.IsNullOrWhiteSpace(_input.text) && _service.Send(_input.text))
            _input.text = string.Empty;
        _input.ActivateInputField();
    }

    private void OnFocusDropdown(int index)
    {
        if (index >= 0 && index < _ordered.Count)
            _service.SetFocus(_ordered[index]);
    }

    private void ToggleGroup(GroupView gv)
    {
        gv.Collapsed = !gv.Collapsed;
        RebuildChannels();
    }

    private void RebuildChannels()
    {
        var ordered = new List<MenuChatChannel>();

        foreach (var gv in _groups)
        {
            for (int i = gv.Container.childCount - 1; i >= 0; i--)
            {
                var child = gv.Container.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            var list = _service.GetChannels(gv.Group).ToList();
            gv.Root.SetActive(list.Count > 0 || gv.Group == MenuChatGroup.Channels);
            gv.HeaderLabel.text = $"{(gv.Collapsed ? "[+]" : "[-]")} {gv.Title} ({list.Count})";
            gv.Container.gameObject.SetActive(!gv.Collapsed);

            foreach (var ch in list)
            {
                var btn = Instantiate(_channelButtonPrefab, gv.Container);
                var label = btn.GetComponentInChildren<TMP_Text>();

                string title = Escape(ch.Title);
                if (ch.Type == MenuChatChannelType.Private)
                    title = _service.IsOnline(ch) ? $"<color=#8BE28B>{title}</color>" : $"<color=#9A9A9A>{title}</color>";
                if (ch == _service.Active) title = $"<b>{title}</b>";
                if (ch.Unread > 0) title += $" <color=#FFB000>({ch.Unread})</color>";
                label.text = title;

                var captured = ch;
                btn.onClick.AddListener(() => _service.SetActive(captured));
                ordered.Add(ch);
            }
        }

        _ordered = ordered;

        _focusDropdown.ClearOptions();
        _focusDropdown.AddOptions(ordered.Select(c => c.Title).ToList());
        SetDropdownValue(_service.Focus);
    }

    private void SetDropdownValue(MenuChatChannel focus)
    {
        int idx = _ordered.IndexOf(focus);
        _focusDropdown.SetValueWithoutNotify(Mathf.Max(idx, 0));
        _focusDropdown.RefreshShownValue();
    }
    
    private void OnActiveChanged(MenuChatChannel ch) => RebuildHistory();
    private void OnFocusChanged(MenuChatChannel ch) => SetDropdownValue(ch);
    private void OnHistoryLoaded(MenuChatChannel ch) => RebuildHistory();

    private void OnMessageAdded(MenuChatChannel ch, MenuChatMessage msg)
    {
        AddHistoryItem(msg);
        ScrollToBottom();
    }

    private void OnMessageUpdated(MenuChatChannel ch, MenuChatMessage msg)
    {
        if (ch == _service.Active)
            RebuildHistory();
    }

    private void RebuildHistory()
    {
        for (int i = _historyContent.childCount - 1; i >= 0; i--)
        {
            var child = _historyContent.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }

        var active = _service.Active;
        if (active != null)
            foreach (var m in active.Messages)
                AddHistoryItem(m);

        ScrollToBottom();
    }

    private void AddHistoryItem(MenuChatMessage m)
    {
        var item = Instantiate(_messagePrefab, _historyContent);
        item.text = Format(m);
    }

    private static string Format(MenuChatMessage m)
    {
        if (m.IsSystem)
            return $"<i><color=#999999>{Escape(m.Text)}</color></i>";

        string time = m.Time.ToLocalTime().ToString("HH:mm");

        string suffix = string.Empty;
        switch (m.State)
        {
            case MenuChatMessageState.Pending:
                suffix = " <color=#888888>…</color>";
                break;
            case MenuChatMessageState.Failed:
                suffix = $" <color=#E57373>(не доставлено: {Escape(m.Error)})</color>";
                break;
            default:
                if (m.IsMine && !m.Delivered)
                    suffix = " <color=#888888>(получатель не в сети)</color>";
                break;
        }

        return $"<color=#888888>[{time}]</color> <b>{Escape(m.SenderLogin)}</b>: {Escape(m.Text)}{suffix}";
    }

    private static string Escape(string s)
    {
        return string.IsNullOrEmpty(s) ? string.Empty : s.Replace("<", "<noparse><</noparse>");
    }

    private void ScrollToBottom()
    {
        if (isActiveAndEnabled)
            StartCoroutine(ScrollToBottomNextFrame());
    }

    private IEnumerator ScrollToBottomNextFrame()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        _historyScroll.verticalNormalizedPosition = 0f;
    }
}