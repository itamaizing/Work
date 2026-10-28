using UnityEngine;

namespace UIWidget
{
    public class UIWidget_Panel : MonoBehaviour
    {
        [SerializeField] private RectTransform _panel;
        [SerializeField] private RectTransform _confineArea;
        [SerializeReference, SubclassSelector] private UIWidget_DragBehaviour _dragBehaviour;
        [SerializeReference, SubclassSelector] private UIWidget_ResizeBehaviour _resizeBehaviour;

        private readonly Vector3[] _corners = new Vector3[4];

        public RectTransform ConfineArea { get => _confineArea; }
        public RectTransform Panel { get => _panel; }
        public Vector3[] Corners => _corners;

        private void Awake()
        {
            if (_confineArea == null)
                _confineArea = transform.root as RectTransform;

            _dragBehaviour.Init(this);
            _resizeBehaviour.Init(this);
        }

        private void OnEnable()
        {
            _dragBehaviour.Init(this);
            _resizeBehaviour.Init(this);
        }

        private void OnDisable()
        {
            _dragBehaviour.Dispose();
            _resizeBehaviour.Dispose();
        }


        public void ClampToParent()
        {
            if (_confineArea == null) return;

            Rect bounds = _confineArea.rect;
            _panel.GetWorldCorners(_corners);
            Vector2 min = _confineArea.InverseTransformPoint(_corners[0]);
            Vector2 max = _confineArea.InverseTransformPoint(_corners[2]);

            Vector2 shift = Vector2.zero;

            if (min.x < bounds.xMin) shift.x = bounds.xMin - min.x;
            else if (max.x > bounds.xMax) shift.x = bounds.xMax - max.x;

            if (min.y < bounds.yMin) shift.y = bounds.yMin - min.y;
            else if (max.y > bounds.yMax) shift.y = bounds.yMax - max.y;

            if (shift != Vector2.zero)
                _panel.anchoredPosition += shift;
        }
    }
}