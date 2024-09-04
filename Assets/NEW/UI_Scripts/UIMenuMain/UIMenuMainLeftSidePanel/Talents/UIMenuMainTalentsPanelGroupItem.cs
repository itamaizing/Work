using Mirror;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIMenuMainTalentsPanelGroupItem : MonoBehaviour
{
    public event UnityAction<TalentData> Selected;
    
    [ReadOnly,ShowInInspector]
    public UIMenuMainTalentsPanelGroup Owner;
    
    [SerializeField] private UITwoStates activeState;
    [SerializeField] private Image activeImage;
    [SerializeField] private Image nonActiveImage;
    
    private TalentData _talent;

    private void Update()
    {
        if(_talent == null) return;
        
        activeState.isActive = _talent.IsOpen;
    }

    public void Fill(TalentData talent)
    {
        activeImage.sprite = talent.Icon;
        nonActiveImage.sprite = talent.Icon;
        _talent = talent;
    }
    
    public void Select()
    {
        _talent.IsOpen = !_talent.IsOpen;
        
        Selected?.Invoke(_talent);
    }
    
}
