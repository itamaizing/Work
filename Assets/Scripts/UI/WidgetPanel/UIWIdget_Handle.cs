using HeathenEngineering.Events;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using static ChatWindowHandle;

namespace UIWidget
{
    public class UIWidget_Handle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public event Action<PointerEventData> DragStart;
        public event Action<PointerEventData> DragActive;
        public event Action<PointerEventData> DragEnd;

        public event Action<PointerEventData> HoverStart;
        public event Action<PointerEventData> HoverEnd;

        public void OnBeginDrag(PointerEventData eventData) => DragStart?.Invoke(eventData);

        public void OnDrag(PointerEventData eventData) => DragActive?.Invoke(eventData);

        public void OnEndDrag(PointerEventData eventData) => DragEnd?.Invoke(eventData);

        public void OnPointerEnter(PointerEventData eventData) => HoverStart?.Invoke(eventData);

        public void OnPointerExit(PointerEventData eventData) => HoverEnd?.Invoke(eventData);
    }
}