using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChatWindowHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
{
    public enum HandleMode { Move, Resize }

    [SerializeField] private ChatWindowFrame _frame;
    [SerializeField] private HandleMode _mode = HandleMode.Move;
    
    [SerializeField] private ScrollRect _forwardScrollTo;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_mode == HandleMode.Move) _frame.BeginMove(eventData);
        else _frame.BeginResize(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_mode == HandleMode.Move) _frame.Move(eventData);
        else _frame.Resize(eventData);
    }

    public void OnEndDrag(PointerEventData eventData) => _frame.EndInteraction();

    public void OnScroll(PointerEventData eventData)
    {
        if (_forwardScrollTo != null)
            _forwardScrollTo.OnScroll(eventData);
    }
}