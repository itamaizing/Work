using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class ChatWindowFrame : MonoBehaviour
{
    [SerializeField] private RectTransform _dragZone;
    [SerializeField] private RectMask2D unfocusedFade;
    [SerializeField, Range(0.1f, 1f)] private float _dragZoneHeightRatio = 0.5f;

    [SerializeField] private Vector2 _minSize = new Vector2(400f, 300f);

    [Header("Persistence")]
    [SerializeField] private string _prefsKey = "ChatWindow";

    private RectTransform _rect;
    private RectTransform _parent;
    private readonly Vector3[] _corners = new Vector3[4];

    private Vector3 _moveGrabOffset;
    private Vector2 _resizeGrabOffset;

    private void Awake()
    {
        _rect = (RectTransform)transform;
        _parent = _rect.parent as RectTransform;

        ConfigureDragZone();
        LoadLayout();
        RecalcFade();
    }

    private void Start()
    {
        ClampToParent();
    }

    private void ConfigureDragZone()
    {
        if (_dragZone == null) return;

        _dragZone.anchorMin = new Vector2(0f, 1f - _dragZoneHeightRatio);
        _dragZone.anchorMax = new Vector2(1f, 1f);
        _dragZone.offsetMin = Vector2.zero;
        _dragZone.offsetMax = Vector2.zero;
    }

    #region Move

    public void BeginMove(PointerEventData e)
    {
        if (TryGetPointerWorld(e, out Vector3 world))
            _moveGrabOffset = _rect.position - world;
    }

    public void Move(PointerEventData e)
    {
        if (!TryGetPointerWorld(e, out Vector3 world)) return;

        _rect.position = world + _moveGrabOffset;
        ClampToParent();
    }

    private bool TryGetPointerWorld(PointerEventData e, out Vector3 world) =>
        RectTransformUtility.ScreenPointToWorldPointInRectangle(_parent, e.position, e.pressEventCamera, out world);

    #endregion

    #region Resize

    public void BeginResize(PointerEventData e)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, e.position, e.pressEventCamera, out Vector2 local))
        {
            Rect r = _rect.rect;
            _resizeGrabOffset = new Vector2(r.xMax, r.yMin) - local;
        }
    }

    public void Resize(PointerEventData e)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, e.position, e.pressEventCamera, out Vector2 local))
            return;

        Rect r = _rect.rect;
        Vector2 corner = local + _resizeGrabOffset;
        Vector2 desired = new Vector2(corner.x - r.xMin, r.yMax - corner.y);

        Vector2 topLeft = _parent.InverseTransformPoint(_rect.TransformPoint(new Vector2(r.xMin, r.yMax)));
        Vector2 maxSize = new Vector2(
            _parent.rect.xMax - topLeft.x,
            topLeft.y - _parent.rect.yMin);

        Vector2 newSize = new Vector2(
            Mathf.Clamp(desired.x, _minSize.x, Mathf.Max(_minSize.x, maxSize.x)),
            Mathf.Clamp(desired.y, _minSize.y, Mathf.Max(_minSize.y, maxSize.y)));

        ApplySize(newSize);
    }

    private void ApplySize(Vector2 newSize)
    {
        Vector2 applied = newSize - _rect.rect.size;

        _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newSize.x);
        _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newSize.y);

        _rect.anchoredPosition += new Vector2(
            applied.x * _rect.pivot.x,
            -applied.y * (1f - _rect.pivot.y));
    }

    private void RecalcFade()
    {
        int border = (int)(_rect.rect.size.y / 3);
        unfocusedFade.softness = new Vector2Int (0, border);
        unfocusedFade.padding = new Vector4 (0, -border, 0, border); //left, btm, right, top
    }

    #endregion

    public void EndInteraction()
    {
        ClampToParent();
        RecalcFade();
        SaveLayout();
    }

    private void ClampToParent()
    {
        if (_parent == null) return;

        Rect bounds = _parent.rect;
        _rect.GetWorldCorners(_corners);
        Vector2 min = _parent.InverseTransformPoint(_corners[0]);
        Vector2 max = _parent.InverseTransformPoint(_corners[2]);

        Vector2 shift = Vector2.zero;

        if (min.x < bounds.xMin) shift.x = bounds.xMin - min.x;
        else if (max.x > bounds.xMax) shift.x = bounds.xMax - max.x;

        if (min.y < bounds.yMin) shift.y = bounds.yMin - min.y;
        else if (max.y > bounds.yMax) shift.y = bounds.yMax - max.y;

        if (shift != Vector2.zero)
            _rect.anchoredPosition += shift;
    }

    private void SaveLayout()
    {
        Vector2 pos = _rect.anchoredPosition;
        Vector2 size = _rect.rect.size;

        PlayerPrefs.SetFloat(_prefsKey + "_x", pos.x);
        PlayerPrefs.SetFloat(_prefsKey + "_y", pos.y);
        PlayerPrefs.SetFloat(_prefsKey + "_w", size.x);
        PlayerPrefs.SetFloat(_prefsKey + "_h", size.y);
        PlayerPrefs.Save();
    }

    private void LoadLayout()
    {
        if (!PlayerPrefs.HasKey(_prefsKey + "_w")) return;

        float w = Mathf.Max(_minSize.x, PlayerPrefs.GetFloat(_prefsKey + "_w"));
        float h = Mathf.Max(_minSize.y, PlayerPrefs.GetFloat(_prefsKey + "_h"));

        _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
        _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
        _rect.anchoredPosition = new Vector2(
            PlayerPrefs.GetFloat(_prefsKey + "_x"),
            PlayerPrefs.GetFloat(_prefsKey + "_y"));
    }
}