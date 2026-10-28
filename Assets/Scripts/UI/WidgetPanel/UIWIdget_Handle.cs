using System;
using UnityEngine;
using UnityEngine.EventSystems;
using static ChatWindowHandle;

namespace UIWidget
{
    public class UIWidget_Handle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public event Action<PointerEventData> DragStart;
        public event Action<PointerEventData> DragActive;
        public event Action<PointerEventData> DragEnd;

        public void OnBeginDrag(PointerEventData eventData) => DragStart?.Invoke(eventData);

        public void OnDrag(PointerEventData eventData) => DragActive?.Invoke(eventData);

        public void OnEndDrag(PointerEventData eventData) => DragEnd?.Invoke(eventData);

    }

    //[System.Serializable]
    //public class UIWidget_DragHandle : UIWidget_Handle
    //{

    //    //[SerializeField] private ChatWindowFrame _frame;
    //    //[SerializeField] private ScrollRect _forwardScrollTo;



    //}

    //[System.Serializable]
    //public class UIWidget_ResizeBehaviour : UIWidget_Handle
    //{

    //    [SerializeField] private ChatWindowFrame _frame;
    //    [SerializeField] private HandleMode _mode = HandleMode.Move;

    //    [SerializeField] private ScrollRect _forwardScrollTo;

    //    public void OnBeginDrag(PointerEventData eventData)
    //    {
    //        if (_mode == HandleMode.Move) _frame.BeginMove(eventData);
    //        else _frame.BeginResize(eventData);
    //    }

    //    public void OnDrag(PointerEventData eventData)
    //    {
    //        if (_mode == HandleMode.Move) _frame.Move(eventData);
    //        else _frame.Resize(eventData);
    //    }

    //    public void OnEndDrag(PointerEventData eventData) => _frame.EndInteraction();

    //}

    //[System.Serializable]
    //public class UIWidget_ResizeBorder : UIWidget_ResizeBehaviour
    //{

    //    [SerializeField] private ChatWindowFrame _frame;
    //    [SerializeField] private HandleMode _mode = HandleMode.Move;

    //    [SerializeField] private ScrollRect _forwardScrollTo;

    //    public void OnBeginDrag(PointerEventData eventData)
    //    {
    //        if (_mode == HandleMode.Move) _frame.BeginMove(eventData);
    //        else _frame.BeginResize(eventData);
    //    }

    //    public void OnDrag(PointerEventData eventData)
    //    {
    //        if (_mode == HandleMode.Move) _frame.Move(eventData);
    //        else _frame.Resize(eventData);
    //    }

    //    public void OnEndDrag(PointerEventData eventData) => _frame.EndInteraction();

    //}

    //[System.Serializable]
    //public class UIWidget_ResizeCorner : UIWidget_ResizeBehaviour
    //{

    //    [SerializeField] private ChatWindowFrame _frame;
    //    [SerializeField] private HandleMode _mode = HandleMode.Move;

    //    [SerializeField] private ScrollRect _forwardScrollTo;

    //    public void OnBeginDrag(PointerEventData eventData)
    //    {
    //        if (_mode == HandleMode.Move) _frame.BeginMove(eventData);
    //        else _frame.BeginResize(eventData);
    //    }

    //    public void OnDrag(PointerEventData eventData)
    //    {
    //        if (_mode == HandleMode.Move) _frame.Move(eventData);
    //        else _frame.Resize(eventData);
    //    }

    //    public void OnEndDrag(PointerEventData eventData) => _frame.EndInteraction();

    //}
}