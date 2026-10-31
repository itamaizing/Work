using System;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SocialPlatforms;

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


    public class UIWidget_AllowResize : UIWidget_ResizeBehaviour
    {
        protected class HandleEvents
        {
            public Action<PointerEventData> Start;
            public Action<PointerEventData> Active;
            public Action<PointerEventData> End;
            public Action<PointerEventData> HoverStart;
            public Action<PointerEventData> HoverEnd;
        }

        [Serializable]
        protected class SizeClamping
        {
            public Vector2 Min = Vector2.zero;
            public Vector2 Max = Vector2.zero;
        }

        protected enum AxisAnchor { Min, Center, Max }

        public enum ResizeType { Resize, Scale, ResizeAndScale }


        [SerializeField] protected ResizeType _resizeType;
        [SerializeField] protected float _borderSize = 0;
        [SerializeField] protected float _cornerSize = 0;
        [SerializeField] protected SizeClamping _sizeClamp;
        [SerializeField] private bool _resizingPreserveAspectRatio = false;
        [SerializeField] protected SizeClamping _scaleClamp;
        [SerializeField] private bool _scalingPreserveAspectRatio = false;
        [SerializeField] protected Dictionary<ResizeHandleType, List<UIWidget_Handle>> _resizeHandles;

        protected UIWidget_Panel _widget;
        bool cursorLockState = false;
        protected AxisAnchor _anchorX;
        protected AxisAnchor _anchorY;
        protected Vector3 _grabWorld;
        protected Vector3 _anchorWorld;
        protected Vector3 _baseScale;
        protected float _baseVisualW;
        protected float _baseVisualH;
        protected float _baseLocalW;
        protected float _baseLocalH;
        protected Dictionary<UIWidget_Handle, HandleEvents> _resizeEvents = new();

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
                    events.HoverStart = evt => HandleHoverStart(evt, TypeList.Key);
                    events.HoverEnd = evt => HandleHoverEnd(evt, TypeList.Key);

                    handle.DragStart += events.Start;
                    handle.DragActive += events.Active;
                    handle.DragEnd += events.End;
                    handle.HoverStart += events.HoverStart;
                    handle.HoverEnd += events.HoverEnd;

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
                handleEvent.Key.HoverStart += handleEvent.Value.HoverStart;
                handleEvent.Key.HoverEnd += handleEvent.Value.HoverEnd;

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

        protected virtual void SetUpAnchors(Vector2 local, ResizeHandleType type)
        {
            Rect r = Panel.rect;

            _baseScale = Panel.localScale;
            Vector3 lossy = Panel.lossyScale;

            _baseLocalW = r.width;
            _baseLocalH = r.height;
            _baseVisualW = r.width * Mathf.Abs(lossy.x);
            _baseVisualH = r.height * Mathf.Abs(lossy.y);

            bool allowX = type is ResizeHandleType.Corner or ResizeHandleType.BorderHorizontal;
            bool allowY = type is ResizeHandleType.Corner or ResizeHandleType.BorderVertical;

            _anchorX = !allowX ? AxisAnchor.Center
                      : local.x < r.center.x ? AxisAnchor.Max
                                             : AxisAnchor.Min;
            _anchorY = !allowY ? AxisAnchor.Center
                      : local.y < r.center.y ? AxisAnchor.Max
                                             : AxisAnchor.Min;

            _anchorWorld = Panel.TransformPoint(GetAnchorLocal(r));
        }

        protected Vector2 GetAnchorLocal(Rect r) => new(
            _anchorX == AxisAnchor.Max ? r.xMax : _anchorX == AxisAnchor.Min ? r.xMin : r.center.x,
            _anchorY == AxisAnchor.Max ? r.yMax : _anchorY == AxisAnchor.Min ? r.yMin : r.center.y);

        protected virtual void HandleResizeStart(PointerEventData e, ResizeHandleType type)
        {
            cursorLockState = true;
            
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    Panel, e.position, e.pressEventCamera, out Vector2 local))
                return;

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                Panel, e.position, e.pressEventCamera, out Vector3 grabWorld))
                _grabWorld = grabWorld;
            SetUpAnchors(local, type);
            ApplyCursor(e, type);
        }

        protected virtual void HandleResizeActive(PointerEventData e, ResizeHandleType type)
        {
            switch (_resizeType)
            {
                case ResizeType.Resize:
                    Resizing(e);
                    break;

                case ResizeType.Scale:
                    Scaling(e);
                    break;

                case ResizeType.ResizeAndScale:
                    if (Input.GetKey(KeyCode.LeftControl))
                        Scaling(e);
                    else
                        Resizing(e);
                    break;
            }
        }

        protected virtual void HandleResizeEnd(PointerEventData e, ResizeHandleType type)
        {
            _widget.ClampToParent();
            cursorLockState = false;
            CursorService.SetState(CursorState.Default);
        }

        #region Resizing
        protected void Resizing(PointerEventData e)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    Panel, e.position, e.pressEventCamera, out Vector2 local))
                return;

            Rect r = Panel.rect;
            Vector2 anchorLocal = GetAnchorLocal(r);

            bool allowX = _anchorX != AxisAnchor.Center;
            bool allowY = _anchorY != AxisAnchor.Center;

            float rawW = _baseLocalW;
            float rawH = _baseLocalH;

            if (allowX)
                rawW = Mathf.Abs(local.x - anchorLocal.x);
            if (allowY)
                rawH = Mathf.Abs(local.y - anchorLocal.y);

            Vector2 maxAllowed = ClampSize();
            float minW = _sizeClamp.Min.x, maxW = Mathf.Max(_sizeClamp.Min.x, maxAllowed.x);
            float minH = _sizeClamp.Min.y, maxH = Mathf.Max(_sizeClamp.Min.y, maxAllowed.y);

            bool preserveAspect = _resizingPreserveAspectRatio || Input.GetKey(KeyCode.LeftShift);

            Vector2 target = GetTargetSize(
                rawW, rawH, _baseLocalW, _baseLocalH,
                minW, maxW, minH, maxH,
                allowX, allowY, preserveAspect);

            bool applyX = allowX || preserveAspect;
            bool applyY = allowY || preserveAspect;

            ResizeFromAnchor(new Vector2(
                applyX ? target.x : r.width,
                applyY ? target.y : r.height));
        }

        protected void ResizeFromAnchor(Vector2 newSize)
        {
            Vector3 anchorWorldBefore = Panel.TransformPoint(GetAnchorLocal(Panel.rect));

            Panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newSize.x);
            Panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newSize.y);

            Vector3 anchorWorldAfter = Panel.TransformPoint(GetAnchorLocal(Panel.rect));
            Panel.position += anchorWorldBefore - anchorWorldAfter;
        }

        protected Vector2 ClampSize()
        {
            Vector2 anchorInConfine = _widget.ConfineArea.InverseTransformPoint(
                Panel.TransformPoint(GetAnchorLocal(Panel.rect)));

            Rect bounds = _widget.ConfineArea.rect;

            float maxW = _anchorX switch
            {
                AxisAnchor.Max => anchorInConfine.x - bounds.xMin,
                AxisAnchor.Min => bounds.xMax - anchorInConfine.x,
                _ => 2f * Mathf.Min(anchorInConfine.x - bounds.xMin, bounds.xMax - anchorInConfine.x),
            };
            float maxH = _anchorY switch
            {
                AxisAnchor.Max => anchorInConfine.y - bounds.yMin,
                AxisAnchor.Min => bounds.yMax - anchorInConfine.y,
                _ => 2f * Mathf.Min(anchorInConfine.y - bounds.yMin, bounds.yMax - anchorInConfine.y),
            };

            if (_sizeClamp.Max.x > 0f) maxW = Mathf.Min(maxW, _sizeClamp.Max.x);
            if (_sizeClamp.Max.y > 0f) maxH = Mathf.Min(maxH, _sizeClamp.Max.y);

            return new Vector2(maxW, maxH);
        }
        #endregion

        #region Scaling
        protected void Scaling(PointerEventData e)
        {
            if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    Panel, e.position, e.pressEventCamera, out Vector3 pointerWorld))
                return;

            bool allowX = _anchorX != AxisAnchor.Center;
            bool allowY = _anchorY != AxisAnchor.Center;

            float deltaX = pointerWorld.x - _grabWorld.x;
            float deltaY = pointerWorld.y - _grabWorld.y;

            float rawW = _baseVisualW;
            float rawH = _baseVisualH;

            if (allowX)
            {
                float scaleX = _anchorX switch
                {
                    AxisAnchor.Min => 1f,
                    AxisAnchor.Max => -1f,
                    AxisAnchor.Center => 2f,
                    _ => 1f
                };
                rawW = Mathf.Max(0f, _baseVisualW + scaleX * deltaX);
            }
            if (allowY)
            {
                float scaleY = _anchorY switch
                {
                    AxisAnchor.Min => 1f,
                    AxisAnchor.Max => -1f,
                    AxisAnchor.Center => 2f,
                    _ => 1f
                };
                rawH = Mathf.Max(0f, _baseVisualH + scaleY * deltaY);
            }

            Vector2 maxWorld = GetMaxWorldSize();
            float minW = _scaleClamp.Min.x, maxW = Mathf.Max(_scaleClamp.Min.x, maxWorld.x);
            float minH = _scaleClamp.Min.y, maxH = Mathf.Max(_scaleClamp.Min.y, maxWorld.y);

            bool preserveAspect = _scalingPreserveAspectRatio || Input.GetKey(KeyCode.LeftShift);

            Vector2 target = GetTargetSize(
                rawW, rawH, _baseVisualW, _baseVisualH,
                minW, maxW, minH, maxH,
                allowX, allowY, preserveAspect);

            float ratioFX = _baseVisualW > 1e-6f ? target.x / _baseVisualW : 1f;
            float ratioFY = _baseVisualH > 1e-6f ? target.y / _baseVisualH : 1f;

            Vector3 newScale = new(
                _baseScale.x * ratioFX,
                _baseScale.y * ratioFY,
                _baseScale.z);

            if ((newScale - Panel.localScale).sqrMagnitude < 1e-12f) return;

            Panel.localScale = newScale;
            Vector3 anchorAfter = Panel.TransformPoint(GetAnchorLocal(Panel.rect));
            Panel.position += _anchorWorld - anchorAfter;
        }

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

            if (_scaleClamp.Max.x > 0) maxW = Mathf.Min(maxW, _scaleClamp.Max.x);
            if (_scaleClamp.Max.y > 0) maxH = Mathf.Min(maxH, _scaleClamp.Max.y);

            return new Vector2(maxW, maxH);
        }
        #endregion

        private Vector2 GetTargetSize(
            float rawW, float rawH, float baseW, float baseH,
            float minW, float maxW, float minH, float maxH,
            bool allowX, bool allowY, bool preserve)
        {
            if (!preserve)
            {
                return new Vector2(
                    allowX ? Mathf.Clamp(rawW, minW, maxW) : baseW,
                    allowY ? Mathf.Clamp(rawH, minH, maxH) : baseH);
            }

            float ratioX = baseW > 1e-6f ? rawW / baseW : 1f;
            float ratioY = baseH > 1e-6f ? rawH / baseH : 1f;

            float scaleFactor;
            if (allowX && allowY)
            {
                float deltaX = Mathf.Abs(ratioX - 1f);
                float deltaY = Mathf.Abs(ratioY - 1f);
                scaleFactor = deltaX >= deltaY ? ratioX : ratioY;
            }
            else if (allowX)
                scaleFactor = ratioX;
            else
                scaleFactor = ratioY;

            float rMinX = baseW > 1e-6f ? minW / baseW : 0f;
            float rMaxX = baseW > 1e-6f ? maxW / baseW : float.MaxValue;
            float rMinY = baseH > 1e-6f ? minH / baseH : 0f;
            float rMaxY = baseH > 1e-6f ? maxH / baseH : float.MaxValue;

            float rMin = Mathf.Max(rMinX, rMinY);
            float rMax = Mathf.Min(rMaxX, rMaxY);
            scaleFactor = Mathf.Clamp(scaleFactor, rMin, Mathf.Max(rMin, rMax));

            return new Vector2(baseW * scaleFactor, baseH * scaleFactor);
        }

        protected virtual void HandleHoverStart(PointerEventData e, ResizeHandleType type)
        {
            if (cursorLockState)
                return;
            ApplyCursor(e, type);
        }

        protected virtual void ApplyCursor(PointerEventData e, ResizeHandleType type)
        {
            Rect r = Panel.rect;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                Panel, e.position, e.pressEventCamera, out Vector2 local))
                return;

            switch (type)
            {
                case ResizeHandleType.Corner:
                    bool left = local.x < r.center.x;
                    bool bottom = local.y < r.center.y;
                    CursorService.SetState(left == bottom ? CursorState.ResizeCorner_LB_RT
                        : CursorState.ResizeCorner_RB_LT);
                    break;
                case ResizeHandleType.BorderHorizontal:
                    CursorService.SetState(CursorState.ResizeHorizontal);
                    break;
                case ResizeHandleType.BorderVertical:
                    CursorService.SetState(CursorState.ResizeVertical);
                    break;
            }
        }

        protected virtual void HandleHoverEnd(PointerEventData e, ResizeHandleType type)
        {
            if (cursorLockState)
                return;
            CursorService.SetState(CursorState.Default);
        }
    }
}