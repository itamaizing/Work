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
