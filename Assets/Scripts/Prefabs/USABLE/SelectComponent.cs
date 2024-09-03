using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class SelectComponent : NetworkBehaviour
{
    private readonly UnityEvent _onSelect = null;
    private readonly UnityEvent _onDeselect = null;
    
    private MoveComponent _moveComponent;
    private SkillManager _abilitiesComponent;
    private UIPlayerComponents _uiComponent;
    
    private bool _isCurrentPLayer;

    public Vector3 OffsetInGroup { get; set; }

    public bool IsCurrentPlayer
    {
        get => _isCurrentPLayer;
        set
        {
            _isCurrentPLayer = value;
            if(_isCurrentPLayer) _abilitiesComponent.SetAbilitiesPanelEnable();
        }
    }

    public void Initialize(MoveComponent move, SkillManager abilitiesComponent,UIPlayerComponents uiComponent)
    {
        _moveComponent = move;
        _abilitiesComponent = abilitiesComponent;
        _uiComponent = uiComponent;
    } 
    
    [Client] 
    public void Select()
    {
        if(!isOwned) return;
        
        _uiComponent.ChangeSelection(true);
        _abilitiesComponent.SetAbilitiesPanelSelect(true);
        _abilitiesComponent.OnSelect(true);
        _moveComponent.SetOffset(OffsetInGroup);
        _moveComponent.IsSelect = true;
        
        _onSelect?.Invoke();
    }
    [Client]
    public void Deselect()
    {
        if(!isOwned) return;
        
        _uiComponent.ChangeSelection(false);
        _abilitiesComponent.SetAbilitiesPanelSelect(false);
        _abilitiesComponent.OnSelect(false);
        _moveComponent.SetOffset(OffsetInGroup);
        _moveComponent.IsSelect = false;
        
        _onDeselect?.Invoke();
    }
    
}