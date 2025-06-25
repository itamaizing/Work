using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIObjectComponents : MonoBehaviour
{
    [SerializeField] private Object _object;
    [SerializeField] private SelectedCircle CircleSelect;
    [SerializeField] private MinimapMarker MarkersSelect;

    public void ChangeSelection(bool isSelect)
    {
        CircleSelect.IsActive = isSelect;
        CircleSelect.SetColorTarget(Color.green);
        MarkersSelect.IsActive = isSelect;
    }
}

