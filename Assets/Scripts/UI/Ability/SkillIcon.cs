using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillIcon : MonoBehaviour, IDropHandler
{
    [SerializeField] private Image _boxFrame;
    [SerializeField] private TMP_Text _key;

    private DraggableIcon _currentIcon;

    public DraggableIcon CurrentIcon { get => _currentIcon; set => _currentIcon = value; }
    public TMP_Text Key { get => _key; }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;
        DraggableIcon draggableIcon = dropped.GetComponent<DraggableIcon>();

        if(_currentIcon == null)    
        {
            draggableIcon.PatentAfterDrag = transform;
            _currentIcon = draggableIcon;
        }
        else 
        {
            _currentIcon.PatentAfterDrag = draggableIcon.PatentAfterDrag;
            _currentIcon.OnEndDrag(null);
            draggableIcon.PatentAfterDrag = transform;
            _currentIcon = draggableIcon;
        }
    }
}
