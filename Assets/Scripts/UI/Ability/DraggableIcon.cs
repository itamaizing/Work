using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableIcon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] Image _image;
    [SerializeField] private FillAmountOverTime _cooldown;
    [SerializeField] private TextMeshProUGUI _chargeCounter;

    private Transform _patentAfterDrag;

    public Transform PatentAfterDrag { get => _patentAfterDrag; set => _patentAfterDrag = value; }

    public void OnBeginDrag(PointerEventData eventData)
    {
        PatentAfterDrag = transform.parent;
        PatentAfterDrag.GetComponent<SkillIcon>().CurrentIcon = null;
        transform.SetParent(transform.root);
        transform.SetAsLastSibling();
        _image.raycastTarget = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        transform.SetParent(PatentAfterDrag);
        transform.SetAsFirstSibling();
        _image.raycastTarget = true;
        PatentAfterDrag.GetComponent<SkillIcon>().CurrentIcon = this;
    }
}
