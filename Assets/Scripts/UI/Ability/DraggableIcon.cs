using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableIcon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image _image;

    private Transform _patentAfterDrag;
    private Skill _skill;
    private bool _selected;

    public Transform PatentAfterDrag { get => _patentAfterDrag; set => _patentAfterDrag = value; }
    public Skill Skill { get => _skill; set => _skill = value; }
    public bool Selected { get => _selected; set => _selected = value; }

    public event Action BeginDrag;
    public event Action EndDrag;

    public void Init(Skill skill, Transform parent)
    {
        _skill = skill;
        _image.sprite = _skill.Icon;
        PatentAfterDrag = parent;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_skill.IsCasting)
            return;

        PatentAfterDrag = transform.parent;
        PatentAfterDrag.GetComponent<SkillIcon>().CurrentIcon = null;
        transform.SetParent(transform.root);
        transform.SetAsLastSibling();
        _image.raycastTarget = false;

        BeginDrag?.Invoke();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_skill.IsCasting)
            return;

        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        transform.SetParent(PatentAfterDrag);
        transform.SetAsFirstSibling();
        _image.raycastTarget = true;
        PatentAfterDrag.GetComponent<SkillIcon>().CurrentIcon = this;

        EndDrag?.Invoke();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        InputHandler.OnSwitchAutoMode += OnClickWithCtrl;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        InputHandler.OnSwitchAutoMode -= OnClickWithCtrl;
    }

    private void OnClickWithCtrl()
    {
        if (Skill is AutoAttackSkill autuAttackSkill)
        {
            autuAttackSkill.SwitchAutoMode();
            Debug.Log("AA mode - " + autuAttackSkill.IsAutoattackMode);
        }
    }
}
