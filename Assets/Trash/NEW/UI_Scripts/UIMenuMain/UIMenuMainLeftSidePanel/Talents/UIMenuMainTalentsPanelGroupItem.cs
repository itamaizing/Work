using Mirror;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIMenuMainTalentsPanelGroupItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public event UnityAction<TalentData, bool> Selected;
    public event UnityAction<TalentData> PointerEntered;
    public event UnityAction<TalentData> PointerExited;
    
    [ReadOnly,ShowInInspector]
    public UIMenuMainTalentsPanelGroup Owner;
    
    [SerializeField] private UITwoStates activeState;
    [SerializeField] private Image activeImage;
    [SerializeField] private Image nonActiveImage;
    
    private TalentData _talent;

    public void Fill(TalentData talent)
    {
        activeImage.sprite = talent.Icon;
        nonActiveImage.sprite = talent.Icon;
        _talent = talent;
        
        activeState.isActive = _talent.IsOpen;
    }
    
    public void Select()
    {
        Debug.Log("Talent selected in talent " +  _talent);
        Selected?.Invoke(_talent, !_talent.IsOpen);
        activeState.isActive = _talent.IsOpen;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PointerEntered?.Invoke(_talent);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PointerExited?.Invoke(_talent);
    }
}
