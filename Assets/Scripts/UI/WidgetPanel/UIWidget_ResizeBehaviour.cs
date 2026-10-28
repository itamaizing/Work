using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UIWidget
{
    public enum ResizeHandleType
    {
        Corner,
        BorderHorizontal,
        BorderVertical,
    }

    [Serializable]
    public abstract class UIWidget_ResizeBehaviour : IDisposable
    {
        public virtual void Dispose() { }

        public virtual void Init(UIWidget_Panel panel) { }
    }


    public abstract class UIWidget_AllowResize : UIWidget_ResizeBehaviour
    {
        protected class HandleEvents
        {
            public Action<PointerEventData> Start;
            public Action<PointerEventData> Active;
            public Action<PointerEventData> End;
        }
        
        [SerializeField] protected float _borderSize = 0;
        [SerializeField] protected float _cornerSize = 0;
        [SerializeField] protected Vector2 _minSize = Vector2.zero;
        [SerializeField] protected Vector2 _maxSize = Vector2.zero;

        [SerializeField] protected Dictionary<ResizeHandleType, List<UIWidget_Handle>> _resizeHandles;

        protected Dictionary<UIWidget_Handle, HandleEvents> _resizeEvents = new();
        protected UIWidget_Panel _widget;

        protected RectTransform Panel => _widget.Panel;
        
        #region Initialization
        public override void Init(UIWidget_Panel panel)
        {
            _widget = panel;
            if (_borderSize != 0 || _cornerSize != 0)
                SetUpBorderSize();
            SubscribeToHandles();
        }

        public override void Dispose()
        {
            UnsubscribeFromHandles();
        }

        private void SubscribeToHandles()
        {
            foreach (var TypeList in _resizeHandles)
                foreach (var handle in TypeList.Value)
                {
                    if (_resizeEvents.ContainsKey(handle))
                        continue;
                    HandleEvents events = new();
                    events.Start = evt => HandleResizeStart(evt, TypeList.Key);
                    events.Active = evt => HandleResizeActive(evt, TypeList.Key);
                    events.End = evt => HandleResizeEnd(evt, TypeList.Key);

                    handle.DragStart += events.Start;
                    handle.DragActive += events.Active;
                    handle.DragEnd += events.End;

                    _resizeEvents[handle] = events;
                }
        }

        private void UnsubscribeFromHandles()
        {
            foreach (var handleEvent in _resizeEvents)
            {
                handleEvent.Key.DragStart -= handleEvent.Value.Start;
                handleEvent.Key.DragActive -= handleEvent.Value.Active;
                handleEvent.Key.DragEnd -= handleEvent.Value.End;

                _resizeEvents.Clear();
            }
        }

        private void SetUpBorderSize()
        {
            foreach (var kvp in _resizeHandles)
                foreach (var handle in kvp.Value)
                {
                    RectTransform rect = handle.transform as RectTransform;
                    float cornerSize = Mathf.Max(_borderSize, _cornerSize);

                    switch (kvp.Key)
                    {
                        case ResizeHandleType.BorderHorizontal:
                            rect.sizeDelta = new Vector2(_borderSize != 0 ? _borderSize : rect.sizeDelta.x,
                                -cornerSize);
                            break;

                        case ResizeHandleType.BorderVertical:
                            rect.sizeDelta = new Vector2(-cornerSize,
                                _borderSize != 0 ? _borderSize : rect.sizeDelta.y);
                            break;

                        case ResizeHandleType.Corner:
                            rect.sizeDelta = new Vector2(cornerSize, cornerSize);
                            break;
                    }
                }
        }
        #endregion

        protected virtual void HandleResizeStart(PointerEventData e, ResizeHandleType type) { }
        protected virtual void HandleResizeActive(PointerEventData e, ResizeHandleType type) { }
        protected virtual void HandleResizeEnd(PointerEventData e, ResizeHandleType type)
        {
            _widget.ClampToParent();
        }
    }

    public class UIWidget_Resize : UIWidget_AllowResize
    {
        private bool _anchorIsRight;
        private bool _anchorIsTop;
        private bool _allowResizeX;
        private bool _allowResizeY;


        private void SetUpAnchors(Vector2 local, ResizeHandleType type)
        {
            Rect r = Panel.rect;
            _allowResizeX = type is ResizeHandleType.Corner or ResizeHandleType.BorderHorizontal;
            _allowResizeY = type is ResizeHandleType.Corner or ResizeHandleType.BorderVertical;

            if (_allowResizeX)
                _anchorIsRight = local.x < r.center.x;
            if (_allowResizeY)
                _anchorIsTop = local.y < r.center.y;
        }

        protected override void HandleResizeStart(PointerEventData e, ResizeHandleType type)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    Panel, e.position, e.pressEventCamera, out Vector2 local))
                return;

            SetUpAnchors(local, type);
        }

        protected override void HandleResizeActive(PointerEventData e, ResizeHandleType type)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    Panel, e.position, e.pressEventCamera, out Vector2 local))
                return;

            Rect r = Panel.rect;

            Vector2 anchorLocal = new Vector2(
                _anchorIsRight ? r.xMax : r.xMin,
                _anchorIsTop ? r.yMax : r.yMin);

            float desiredW = r.width;
            float desiredH = r.height;

            if (_allowResizeX)
                desiredW = Mathf.Abs(local.x - anchorLocal.x);
            if (_allowResizeY)
                desiredH = Mathf.Abs(local.y - anchorLocal.y);

            Vector2 maxAllowed = ClampSize();

            float newW = Mathf.Clamp(desiredW, _minSize.x, Mathf.Max(_minSize.x, maxAllowed.x));
            float newH = Mathf.Clamp(desiredH, _minSize.y, Mathf.Max(_minSize.y, maxAllowed.y));

            Vector2 newSize = new Vector2(
                _allowResizeX ? newW : r.width,
                _allowResizeY ? newH : r.height);

            ResizeFromAnchor(newSize);
        }

        private void ResizeFromAnchor(Vector2 newSize)
        {
            Vector2 oldAnchorLocal = new Vector2(
                _anchorIsRight ? Panel.rect.xMax : Panel.rect.xMin,
                _anchorIsTop ? Panel.rect.yMax : Panel.rect.yMin);
            Vector3 anchorWorldBefore = Panel.TransformPoint(oldAnchorLocal);

            Panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newSize.x);
            Panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newSize.y);

            Vector2 newAnchorLocal = new Vector2(
                _anchorIsRight ? Panel.rect.xMax : Panel.rect.xMin,
                _anchorIsTop ? Panel.rect.yMax : Panel.rect.yMin);
            Vector3 anchorWorldAfter = Panel.TransformPoint(newAnchorLocal);

            Panel.position += anchorWorldBefore - anchorWorldAfter;
        }

        private Vector2 ClampSize()
        {
            Vector2 anchorLocal = new Vector2(
                _anchorIsRight ? Panel.rect.xMax : Panel.rect.xMin,
                _anchorIsTop ? Panel.rect.yMax : Panel.rect.yMin);
            Vector2 anchorInConfine = _widget.ConfineArea.InverseTransformPoint(
                Panel.TransformPoint(anchorLocal));

            Rect bounds = _widget.ConfineArea.rect;

            float maxW = _anchorIsRight
                ? anchorInConfine.x - bounds.xMin
                : bounds.xMax - anchorInConfine.x;
            float maxH = _anchorIsTop
                ? anchorInConfine.y - bounds.yMin
                : bounds.yMax - anchorInConfine.y;

            if (_maxSize.x > 0f) maxW = Mathf.Min(maxW, _maxSize.x);
            if (_maxSize.y > 0f) maxH = Mathf.Min(maxH, _maxSize.y);

            return new Vector2(maxW, maxH);
        }
    }

    public class UIWidget_Scale : UIWidget_AllowResize
    {
        private enum AxisAnchor { Min, Center, Max }

        [SerializeField] private bool _preserveAspectRatio = false;

        private Vector3 _baseScale;
        private float _baseVisualW;
        private float _baseVisualH;
        private Vector3 _anchorWorld;
        private AxisAnchor _anchorX;
        private AxisAnchor _anchorY;

        private void SetUpAnchors(Vector2 local, ResizeHandleType type)
        {
            Rect r = Panel.rect;

            _baseScale = Panel.localScale;
            Vector3 lossy = Panel.lossyScale;
            _baseVisualW = r.width * Mathf.Abs(lossy.x);
            _baseVisualH = r.height * Mathf.Abs(lossy.y);

            switch (type)
            {
                case ResizeHandleType.Corner:
                    _anchorX = local.x < r.center.x ? AxisAnchor.Max : AxisAnchor.Min;
                    _anchorY = local.y < r.center.y ? AxisAnchor.Max : AxisAnchor.Min;
                    break;
                case ResizeHandleType.BorderHorizontal:
                    _anchorX = local.x < r.center.x ? AxisAnchor.Max : AxisAnchor.Min;
                    _anchorY = AxisAnchor.Center;
                    break;
                case ResizeHandleType.BorderVertical:
                    _anchorX = AxisAnchor.Center;
                    _anchorY = local.y < r.center.y ? AxisAnchor.Max : AxisAnchor.Min;
                    break;
            }

            _anchorWorld = Panel.TransformPoint(GetAnchorLocal(r));

        }

        protected override void HandleResizeStart(PointerEventData e, ResizeHandleType type)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    Panel, e.position, e.pressEventCamera, out Vector2 local))
                return;

            SetUpAnchors(local, type);
        }

        protected override void HandleResizeActive(PointerEventData e, ResizeHandleType type)
        {
            if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    Panel, e.position, e.pressEventCamera, out Vector3 pointerWorld))
                return;

            bool allowX = type is ResizeHandleType.Corner or ResizeHandleType.BorderHorizontal;
            bool allowY = type is ResizeHandleType.Corner or ResizeHandleType.BorderVertical;

            float rawW = _baseVisualW;
            float rawH = _baseVisualH;

            if (allowX)
            {
                float d = Mathf.Abs(pointerWorld.x - _anchorWorld.x);
                rawW = _anchorX == AxisAnchor.Center ? 2f * d : d;
            }
            if (allowY)
            {
                float d = Mathf.Abs(pointerWorld.y - _anchorWorld.y);
                rawH = _anchorY == AxisAnchor.Center ? 2f * d : d;
            }

            Vector2 maxWorld = GetMaxWorldSize();
            float minW = _minSize.x, maxW = Mathf.Max(_minSize.x, maxWorld.x);
            float minH = _minSize.y, maxH = Mathf.Max(_minSize.y, maxWorld.y);

            float targetW, targetH;

            if (_preserveAspectRatio)
            {
                float ratioX = _baseVisualW > 1e-6f ? rawW / _baseVisualW : 1f;
                float ratioY = _baseVisualH > 1e-6f ? rawH / _baseVisualH : 1f;

                float leading =
                    (allowX && allowY) ? (Mathf.Abs(ratioX - 1f) >= Mathf.Abs(ratioY - 1f) ? ratioX : ratioY) :
                    allowX ? ratioX :
                                         ratioY;

                float rMinX = _baseVisualW > 1e-6f ? minW / _baseVisualW : 0f;
                float rMaxX = _baseVisualW > 1e-6f ? maxW / _baseVisualW : float.MaxValue;
                float rMinY = _baseVisualH > 1e-6f ? minH / _baseVisualH : 0f;
                float rMaxY = _baseVisualH > 1e-6f ? maxH / _baseVisualH : float.MaxValue;
                float rMin = Mathf.Max(rMinX, rMinY);
                float rMax = Mathf.Min(rMaxX, rMaxY);
                leading = Mathf.Clamp(leading, rMin, Mathf.Max(rMin, rMax));

                targetW = _baseVisualW * leading;
                targetH = _baseVisualH * leading;
            }
            else
            {
                targetW = Mathf.Clamp(rawW, minW, maxW);
                targetH = Mathf.Clamp(rawH, minH, maxH);
            }

            float ratioFX = _baseVisualW > 1e-6f ? targetW / _baseVisualW : 1f;
            float ratioFY = _baseVisualH > 1e-6f ? targetH / _baseVisualH : 1f;

            Vector3 newScale = new(
                _baseScale.x * ratioFX,
                _baseScale.y * ratioFY,
                _baseScale.z);

            if ((newScale - Panel.localScale).sqrMagnitude < 1e-12f) return;

            Panel.localScale = newScale;
            Vector3 anchorAfter = Panel.TransformPoint(GetAnchorLocal(Panel.rect));
            Panel.position += _anchorWorld - anchorAfter;
        }

        private Vector2 GetAnchorLocal(Rect r) => new(
            _anchorX == AxisAnchor.Max ? r.xMax : _anchorX == AxisAnchor.Min ? r.xMin : r.center.x,
            _anchorY == AxisAnchor.Max ? r.yMax : _anchorY == AxisAnchor.Min ? r.yMin : r.center.y);

        private Vector2 GetMaxWorldSize()
        {
            Vector3[] corners = new Vector3[4];
            _widget.ConfineArea.GetWorldCorners(corners);
            float cMinX = Mathf.Min(corners[0].x, corners[2].x);
            float cMaxX = Mathf.Max(corners[0].x, corners[2].x);
            float cMinY = Mathf.Min(corners[0].y, corners[2].y);
            float cMaxY = Mathf.Max(corners[0].y, corners[2].y);

            float maxW = _anchorX switch
            {
                AxisAnchor.Max => _anchorWorld.x - cMinX,
                AxisAnchor.Min => cMaxX - _anchorWorld.x,
                _ => 2f * Mathf.Min(_anchorWorld.x - cMinX, cMaxX - _anchorWorld.x),
            };
            float maxH = _anchorY switch
            {
                AxisAnchor.Max => _anchorWorld.y - cMinY,
                AxisAnchor.Min => cMaxY - _anchorWorld.y,
                _ => 2f * Mathf.Min(_anchorWorld.y - cMinY, cMaxY - _anchorWorld.y),
            };

            if (_maxSize.x > 0) maxW = Mathf.Min(maxW, _maxSize.x);
            if (_maxSize.y > 0) maxH = Mathf.Min(maxH, _maxSize.y);

            return new Vector2(maxW, maxH);
        }
    }
}