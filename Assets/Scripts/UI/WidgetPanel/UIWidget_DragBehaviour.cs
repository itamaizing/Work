using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UIWidget
{
    [Serializable]
    public abstract class UIWidget_DragBehaviour : IDisposable
    {
        public virtual void Dispose() { }

        public virtual void Init(UIWidget_Panel panel) { }
    }

    public class UIWidget_Drag : UIWidget_DragBehaviour
    {
        [SerializeField] private UIWidget_Handle _moveHandle;
        private Vector3 _grabOffset;

        private UIWidget_Panel _widget;

        public override void Init(UIWidget_Panel panel)
        {
            _widget = panel;
            SubscribeToHandles();
        }

        public override void Dispose()
        {
            UnsubscribeFromHandles();
        }

        private void SubscribeToHandles()
        {
            _moveHandle.DragStart += HandleMoveStart;
            _moveHandle.DragActive += HandleMoveActive;
            _moveHandle.DragEnd += HandleMoveEnd;
        }

        private void UnsubscribeFromHandles()
        {
            _moveHandle.DragStart -= HandleMoveStart;
            _moveHandle.DragActive -= HandleMoveActive;
            _moveHandle.DragEnd -= HandleMoveEnd;
        }

        private bool TryGetPointerWorld(PointerEventData e, out Vector3 world) =>
            RectTransformUtility.ScreenPointToWorldPointInRectangle(_widget.ConfineArea, e.position, e.pressEventCamera, out world);


        private void HandleMoveStart(PointerEventData e)
        {
            if (TryGetPointerWorld(e, out Vector3 world))
                _grabOffset = _widget.Panel.position - world;
        }

        private void HandleMoveActive(PointerEventData e)
        {
            if (!TryGetPointerWorld(e, out Vector3 world)) return;

            _widget.Panel.position = world + _grabOffset;
            _widget.ClampToParent();

        }

        private void HandleMoveEnd(PointerEventData e)
        {
            _widget.ClampToParent();
        }

    }
}