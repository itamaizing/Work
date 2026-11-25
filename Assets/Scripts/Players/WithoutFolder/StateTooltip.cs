using System.Collections;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum StateTooltipIcon { Dispel, School, Duration }

public class StateTooltip : MonoBehaviour
{
    public static StateTooltip Instance { get; private set; }

    [SerializeField] private GameObject _root;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private Canvas _rootCanvas;
    [SerializeField] private Vector2 _mouseOffset = new(16f, -16f);
    [SerializeField] private float _showDelay = 0.4f;
    [SerializeField, Min(0.05f)] private float _refreshInterval = 0.1f;
    [SerializeField] private List<IconBinding> _icons = new();
    [SerializeField, Min(1f)] private float _metadataIconSize = 16f;

    [Serializable]
    public sealed class IconBinding
    {
        public StateTooltipIcon Kind;
        public Sprite Sprite;
    }

    private Coroutine _showRoutine;
    private bool _isShowing;
    private MonoBehaviour _owner;
    private StateBasic _state;
    private float _nextRefresh;
    private readonly Vector3[] _corners = new Vector3[4];
    private TextMeshProUGUI _schoolText;
    private TextMeshProUGUI _dispelText;
    private TextMeshProUGUI _durationText;
    private GameObject _schoolItem;
    private GameObject _durationItem;

    private void Awake()
    {
        Instance = this;
        if (_rootCanvas == null)
            _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
        Hide();
    }

    public void RequestShow(MonoBehaviour owner, StateBasic state)
    {
        Hide();
        if (owner == null || state == null) return;
        _owner = owner;
        _state = state;
        _showRoutine = StartCoroutine(ShowAfterDelay());
    }

    private IEnumerator ShowAfterDelay()
    {
        yield return new WaitForSecondsRealtime(_showDelay);
        _showRoutine = null;
        if (_owner == null || !_owner.isActiveAndEnabled || _state == null)
        {
            Hide();
            yield break;
        }
        _root.SetActive(true);
        _isShowing = true;
        RefreshContent();
        UpdatePositionToMouse();
    }

    private void RefreshContent()
    {
        _nextRefresh = Time.unscaledTime + Mathf.Max(0.05f, _refreshInterval);
        GameDescriptions.StateDescription.GetTooltip(_state, out string name, out string description, includeRemainingTime: false);
        bool hasDescription = !string.IsNullOrEmpty(description);
        bool changed = _nameText.text != name || _descriptionText.text != description ||
            _descriptionText.gameObject.activeSelf != hasDescription;
        _nameText.text = name;
        _descriptionText.text = description;
        _descriptionText.gameObject.SetActive(hasDescription);
        EnsureMetadata();
        string school = GameDescriptions.StateDescription.GetSchoolText(_state.Schools);
        string dispel = GameDescriptions.StateDescription.GetDispelText(_state.dispelType);
        string duration = GameDescriptions.StateDescription.GetDurationText(_state.RemainingDuration, _state.MaxDuration);
        bool hasSchool = !string.IsNullOrEmpty(school);
        bool hasDuration = !string.IsNullOrEmpty(duration);
        changed |= _schoolText.text != school || _dispelText.text != dispel || _schoolItem.activeSelf != hasSchool ||
            _durationText.text != duration || _durationItem.activeSelf != hasDuration;
        _schoolText.text = school;
        _dispelText.text = dispel;
        _schoolItem.SetActive(hasSchool);
        _durationText.text = duration;
        _durationItem.SetActive(hasDuration);
        if (changed) LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
    }

    private void EnsureMetadata()
    {
        if (_dispelText != null) return;
        var row = new GameObject("StateMetadata", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(_nameText.transform.parent, false);
        row.transform.SetSiblingIndex(_nameText.transform.GetSiblingIndex() + 1);
        var layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        CreateMetadataItem(row.transform, StateTooltipIcon.Dispel, out _dispelText);
        _schoolItem = CreateMetadataItem(row.transform, StateTooltipIcon.School, out _schoolText);
        _durationItem = CreateMetadataItem(row.transform, StateTooltipIcon.Duration, out _durationText);
    }

    private GameObject CreateMetadataItem(Transform parent, StateTooltipIcon kind, out TextMeshProUGUI label)
    {
        var item = new GameObject(kind.ToString(), typeof(RectTransform), typeof(HorizontalLayoutGroup));
        item.transform.SetParent(parent, false);
        var layout = item.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 3f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        Sprite sprite = null;
        foreach (var binding in _icons)
            if (binding != null && binding.Kind == kind && binding.Sprite != null) { sprite = binding.Sprite; break; }
        if (sprite != null)
        {
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            icon.transform.SetParent(item.transform, false);
            var image = icon.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var size = icon.GetComponent<LayoutElement>();
            size.minWidth = size.preferredWidth = _metadataIconSize;
            size.minHeight = size.preferredHeight = _metadataIconSize;
        }
        var text = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI));
        text.transform.SetParent(item.transform, false);
        label = text.GetComponent<TextMeshProUGUI>();
        label.font = _descriptionText.font;
        label.fontSharedMaterial = _descriptionText.fontSharedMaterial;
        label.fontSize = _descriptionText.fontSize;
        label.color = _descriptionText.color;
        label.richText = true;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
        return item;
    }

    private void LateUpdate()
    {
        if (!_isShowing) return;
        if (_owner == null || !_owner.isActiveAndEnabled || _state == null)
        {
            Hide();
            return;
        }
        if (Time.unscaledTime >= _nextRefresh) RefreshContent();
        UpdatePositionToMouse();
    }

    private void UpdatePositionToMouse()
    {
        if (_rootCanvas == null) return;

        Camera camera = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera;
        Vector2 pivot = RectTransformUtility.WorldToScreenPoint(camera, _rectTransform.position);

        _rectTransform.GetWorldCorners(_corners);
        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(null, _corners[0]);
        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(null, _corners[2]);
        Vector2 size = topRight - bottomLeft;

        Vector2 mouse = Input.mousePosition;
        Rect bounds = Screen.safeArea;

        float x = mouse.x + _mouseOffset.x;
        float y = mouse.y - _mouseOffset.y + size.y/2;

        if (x + size.x > bounds.xMax)
            x = mouse.x - _mouseOffset.x;

        x = Mathf.Clamp(x, bounds.xMin, bounds.xMax - size.x);
        y = Mathf.Clamp(y, bounds.yMin + size.y, bounds.yMax);

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            gameObject.transform as RectTransform, new Vector2(x, y), camera, out Vector3 worldPoint))
        {
            gameObject.transform.position = worldPoint;
        }
    }

    public void Hide(MonoBehaviour owner)
    {
        if (_owner == owner) Hide();
    }

    private void Hide()
    {
        CancelPendingShow();
        _owner = null;
        _state = null;
        _isShowing = false;
        _root.SetActive(false);
    }

    private void OnDisable() => Hide();

    private void CancelPendingShow()
    {
        if (_showRoutine == null) return;
        StopCoroutine(_showRoutine);
        _showRoutine = null;
    }
}
