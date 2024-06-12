using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerSelectionIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Transform _transform;

    private Vector3 _increasedScale = new Vector3(1.3f, 1.3f, 1);
    private Vector3 _standardScale = new Vector3(1f, 1f, 1);

    public void OnPointerEnter(PointerEventData eventData)
    {
        _transform.localScale = _increasedScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _transform.localScale = _standardScale;
    }
}
