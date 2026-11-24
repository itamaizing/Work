using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    private Coroutine _showRoutine;
    private bool _isShowing;
    private MonoBehaviour _owner;
    private StateBasic _state;
    private float _nextRefresh;
    private readonly Vector3[] _corners = new Vector3[4];

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
        GameDescriptions.StateDescription.GetTooltip(_state, out string name, out string description);
        bool hasDescription = !string.IsNullOrEmpty(description);
        bool changed = _nameText.text != name || _descriptionText.text != description ||
            _descriptionText.gameObject.activeSelf != hasDescription;
        _nameText.text = name;
        _descriptionText.text = description;
        _descriptionText.gameObject.SetActive(hasDescription);
        if (changed) LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
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
