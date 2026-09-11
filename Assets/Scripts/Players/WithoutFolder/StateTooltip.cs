using System.Collections;
using TMPro;
using UnityEngine;

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

    private void Awake()
    {
        Instance = this;
        Hide();
    }

    public void RequestShow(string tooltipName, string description)
    {
        //if (string.IsNullOrEmpty(tooltipName) && string.IsNullOrEmpty(description)) return;

        CancelPendingShow();
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

        UpdatePositionToMouse();
    }

    private void LateUpdate()
    {
        if (_isShowing) UpdatePositionToMouse();
    }

    private void UpdatePositionToMouse()
    {
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            (RectTransform)_rootCanvas.transform,
            Input.mousePosition + (Vector3)_mouseOffset,
            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera,
            out Vector3 worldPoint);

        _rectTransform.position = worldPoint;
    }

    public void Hide()
    {
        CancelPendingShow();
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