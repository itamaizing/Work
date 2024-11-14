using Mirror;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIMenuMainTalentsPanelGroupItem : MonoBehaviour
{
    public event UnityAction<TalentData, bool> Selected;
    
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
        if (TalentPoints())
        {
            Debug.Log("There are not enough talent points to activate!");
            return;
        }

        Owner.Owner.Owner.GetHero().TalentManager.Points--;
        Selected?.Invoke(_talent, !_talent.IsOpen);
        activeState.isActive = _talent.IsOpen;
    }
    

    private bool TalentPoints()
    {
        return Owner.Owner.Owner.GetHero().TalentManager.Points <= 0;
    }
}
