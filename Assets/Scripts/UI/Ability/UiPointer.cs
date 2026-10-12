using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public static class UiPointer
{
    private static readonly List<RaycastResult> _hits = new();

    public static bool IsOverScreenUi()
    {
        var es = EventSystem.current;
        if (es == null) return false;

        _hits.Clear();
        es.RaycastAll(new PointerEventData(es) { position = Input.mousePosition }, _hits);

        foreach (var hit in _hits)
        {
            var go = hit.gameObject;
            var canvas = go.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.WorldSpace) return true;
            if (ExecuteEvents.GetEventHandler<IPointerClickHandler>(go) != null) return true;
            if (ExecuteEvents.GetEventHandler<IPointerDownHandler>(go) != null) return true;
        }
        return false;
    }
}