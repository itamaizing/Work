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

    private Coroutine _showRoutine;
    private bool _isShowing;
    private MonoBehaviour _owner;
    private readonly Vector3[] _corners = new Vector3[4];

    private void Awake()
    {
        Instance = this;
        if (_rootCanvas == null)
            _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
        Hide();
    }

    public void RequestShow(MonoBehaviour owner, string tooltipName, string description)
    {
        Hide();
        _owner = owner;
        _showRoutine = StartCoroutine(ShowAfterDelay(tooltipName, description));
    }

    private IEnumerator ShowAfterDelay(string tooltipName, string description)
    {
        yield return new WaitForSeconds(_showDelay);
        ShowImmediate(tooltipName, description);
    }

    private void ShowImmediate(string tooltipName, string description)
    {
        _root.SetActive(true);
        _isShowing = true;

        _nameText.text = tooltipName;

        bool hasDescription = !string.IsNullOrEmpty(description);
        _descriptionText.gameObject.SetActive(hasDescription);
        if (hasDescription) _descriptionText.text = description;

        LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
        UpdatePositionToMouse();
    }

    private void LateUpdate()
    {
        if (_isShowing) UpdatePositionToMouse();
    }

    private void UpdatePositionToMouse()
    {
        if (_rootCanvas == null) return;

        Camera camera = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera;
        Vector2 pivot = RectTransformUtility.WorldToScreenPoint(camera, _rectTransform.position);
        _rectTransform.GetWorldCorners(_corners);
        Vector2 minOffset = Vector2.positiveInfinity;
        Vector2 maxOffset = Vector2.negativeInfinity;
        foreach (Vector3 corner in _corners)
        {
            Vector2 offset = RectTransformUtility.WorldToScreenPoint(camera, corner) - pivot;
            minOffset = Vector2.Min(minOffset, offset);
            maxOffset = Vector2.Max(maxOffset, offset);
        }

        Vector2 mouse = Input.mousePosition;
        Rect bounds = Screen.safeArea;
        float gapX = Mathf.Abs(_mouseOffset.x);
        float gapY = Mathf.Abs(_mouseOffset.y);
        float x = mouse.x + gapX - minOffset.x;
        float y = mouse.y - gapY - maxOffset.y;

        if (x + maxOffset.x > bounds.xMax)
            x = mouse.x - gapX - maxOffset.x;
        if (y + minOffset.y < bounds.yMin)
            y = mouse.y + gapY - minOffset.y;

        float minX = bounds.xMin - minOffset.x;
        float maxY = bounds.yMax - maxOffset.y;
        x = Mathf.Clamp(x, minX, Mathf.Max(minX, bounds.xMax - maxOffset.x));
        y = Mathf.Clamp(y, Mathf.Min(maxY, bounds.yMin - minOffset.y), maxY);

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            _rectTransform, new Vector2(x, y), camera, out Vector3 worldPoint))
        {
            _rectTransform.position = worldPoint;
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
        _isShowing = false;
        _root.SetActive(false);
    }

    private void CancelPendingShow()
    {
        if (_showRoutine == null) return;
        StopCoroutine(_showRoutine);
        _showRoutine = null;
    }
}
