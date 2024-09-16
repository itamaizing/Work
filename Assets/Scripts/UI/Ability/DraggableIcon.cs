using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableIcon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] Image _image;

    private Transform _patentAfterDrag;
    private Skill _skill;
    private bool _selected;

    public Transform PatentAfterDrag { get => _patentAfterDrag; set => _patentAfterDrag = value; }
    public Skill Skill { get => _skill; set => _skill = value; }
    public bool Selected { get => _selected; set => _selected = value; }

    public void Init(Skill skill)
    {
        _skill = skill;
        _image.sprite = _skill.Icon;
    }

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
