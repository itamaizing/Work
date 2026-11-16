using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UIWidget
{
    public sealed class HudEditMode : MonoBehaviour
    {
        private const string PreferencesKey = "GameHUD.Layout.v1";

        [Serializable]
        private sealed class PanelEntry
        {
            public string Id;
            public UIWidget_Panel Widget;
        }

        [Serializable]
        private struct PanelLayout
        {
            public string Id;
            public Vector3 Position;
            public Vector2 Size;
            public Vector3 Scale;

            public PanelLayout(string id, RectTransform panel)
            {
                Id = id;
                Position = panel.anchoredPosition3D;
                Size = panel.sizeDelta;
                Scale = panel.localScale;
            }

            public void Apply(RectTransform panel)
            {
                panel.anchoredPosition3D = Position;
                panel.sizeDelta = Size;
                panel.localScale = Scale;
            }

            public bool IsValid =>
                float.IsFinite(Position.x) && float.IsFinite(Position.y) && float.IsFinite(Position.z) &&
                float.IsFinite(Size.x) && float.IsFinite(Size.y) &&
                float.IsFinite(Scale.x) && float.IsFinite(Scale.y) && float.IsFinite(Scale.z) &&
                Scale.x != 0 && Scale.y != 0;
        }

        [Serializable]
        private sealed class SavedLayout
        {
            public PanelLayout[] Panels;
        }

        private sealed class ManagedPanel
        {
            public UIWidget_Panel Widget;
            public RectTransform OriginalParent;
            public int OriginalSiblingIndex;
            public Vector3 WidgetPosition;
            public PanelLayout DefaultLayout;
            public RectTransform EditParent;
        }

        [SerializeField] private PanelEntry[] _panels;
        [SerializeField] private Key _toggleKey = Key.F9;
        [SerializeField] private GameObject _settingsMenu;
        [SerializeField] private RectTransform _editOverlay;
        [SerializeField] private RectTransform _widgetLayer;
        [SerializeField] private Button _editButton;
        [SerializeField] private Button _resetButton;
        [SerializeField] private Button _doneButton;

        private readonly Dictionary<string, ManagedPanel> _managedPanels = new();
        private readonly Dictionary<string, PanelLayout> _savedPanels = new();
        private IDisposable _inputBlock;

        public bool IsEditing => _inputBlock != null;

        private void Awake()
        {
            _editOverlay.gameObject.SetActive(false);
            foreach (var entry in _panels)
            {
                if (entry == null || entry.Widget == null || string.IsNullOrEmpty(entry.Id))
                    continue;

                var widget = entry.Widget;
                if (widget.Panel.IsChildOf(widget.transform) || widget.transform.parent is not RectTransform parent)
                    continue;

                if (!_managedPanels.TryAdd(entry.Id, new ManagedPanel
                {
                    Widget = widget,
                    OriginalParent = parent,
                    OriginalSiblingIndex = widget.transform.GetSiblingIndex(),
                    WidgetPosition = ((RectTransform)widget.transform).anchoredPosition3D,
                    DefaultLayout = new PanelLayout(entry.Id, widget.Panel)
                }))
                    continue;

                widget.gameObject.SetActive(false);
            }

            LoadLayout();
        }

        private void OnEnable()
        {
            _editButton.onClick.AddListener(EnterEditMode);
            _resetButton.onClick.AddListener(ResetLayout);
            _doneButton.onClick.AddListener(ExitEditMode);
        }

        private void OnDisable()
        {
            ExitEditMode();
            _editButton.onClick.RemoveListener(EnterEditMode);
            _resetButton.onClick.RemoveListener(ResetLayout);
            _doneButton.onClick.RemoveListener(ExitEditMode);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (_toggleKey == Key.None || keyboard == null || !keyboard[_toggleKey].wasPressedThisFrame ||
                InputBlocker.IsBlocked(InputLayer.Hotkeys))
                return;

            if (IsEditing)
                ExitEditMode();
            else
                EnterEditMode();
        }

        private void LateUpdate()
        {
            if (!IsEditing) return;

            foreach (var panel in _managedPanels.Values)
                SyncEditParent(panel);
        }

        private void EnterEditMode()
        {
            if (IsEditing) return;

            _inputBlock = InputBlocker.Block(InputLayer.World, "HUD editing");
            InputHandler.OnPlayerMove?.Invoke(Vector2.zero);
            _settingsMenu.SetActive(false);
            _editOverlay.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();

            foreach (var (id, panel) in _managedPanels)
            {
                if (panel.Widget == null || panel.OriginalParent == null) continue;

                if (panel.EditParent == null)
                {
                    panel.EditParent = (RectTransform)new GameObject(id, typeof(RectTransform)).transform;
                    panel.EditParent.SetParent(_widgetLayer, false);
                }

                SyncEditParent(panel);
                var widgetRect = (RectTransform)panel.Widget.transform;
                widgetRect.SetParent(panel.EditParent, false);
                widgetRect.anchoredPosition3D = panel.WidgetPosition;
                panel.Widget.gameObject.SetActive(true);
            }
        }

        public void ExitEditMode()
        {
            if (!IsEditing) return;

            foreach (var (id, panel) in _managedPanels)
            {
                if (panel.Widget == null) continue;

                _savedPanels[id] = new PanelLayout(id, panel.Widget.Panel);
                panel.Widget.gameObject.SetActive(false);
                if (panel.OriginalParent == null) continue;

                var widgetRect = (RectTransform)panel.Widget.transform;
                widgetRect.SetParent(panel.OriginalParent, false);
                widgetRect.anchoredPosition3D = panel.WidgetPosition;
                widgetRect.SetSiblingIndex(panel.OriginalSiblingIndex);
            }
            _editOverlay.gameObject.SetActive(false);
            _inputBlock.Dispose();
            _inputBlock = null;

            if (CursorService.CurrentTheme != null || CursorService.DefaultTheme != null)
                CursorService.SetState(CursorState.Default);

            var layouts = new PanelLayout[_savedPanels.Count];
            _savedPanels.Values.CopyTo(layouts, 0);
            PlayerPrefs.SetString(PreferencesKey, JsonUtility.ToJson(new SavedLayout { Panels = layouts }));
            PlayerPrefs.Save();
        }

        private void ResetLayout()
        {
            if (!IsEditing) return;

            foreach (var panel in _managedPanels.Values)
                if (panel.Widget != null)
                    panel.DefaultLayout.Apply(panel.Widget.Panel);
        }

        private void LoadLayout()
        {
            string json = PlayerPrefs.GetString(PreferencesKey);
            if (string.IsNullOrEmpty(json)) return;

            SavedLayout saved;
            try
            {
                saved = JsonUtility.FromJson<SavedLayout>(json);
            }
            catch (ArgumentException)
            {
                return;
            }

            if (saved?.Panels == null) return;
            foreach (var layout in saved.Panels)
            {
                if (string.IsNullOrEmpty(layout.Id) || !layout.IsValid) continue;

                _savedPanels[layout.Id] = layout;
                if (_managedPanels.TryGetValue(layout.Id, out var panel))
                    layout.Apply(panel.Widget.Panel);
            }
        }

        private void SyncEditParent(ManagedPanel panel)
        {
            var original = panel.OriginalParent;
            var editParent = panel.EditParent;
            if (original == null || editParent == null) return;

            if (editParent.pivot != original.pivot)
                editParent.pivot = original.pivot;
            if (editParent.sizeDelta != original.rect.size)
                editParent.sizeDelta = original.rect.size;
            if (editParent.position != original.position || editParent.rotation != original.rotation)
                editParent.SetPositionAndRotation(original.position, original.rotation);

            Vector3 scale = original.lossyScale;
            Vector3 layerScale = _widgetLayer.lossyScale;
            if (layerScale.x == 0 || layerScale.y == 0 || layerScale.z == 0) return;

            Vector3 localScale = new(scale.x / layerScale.x, scale.y / layerScale.y, scale.z / layerScale.z);
            if (editParent.localScale != localScale)
                editParent.localScale = localScale;
        }
    }
}
