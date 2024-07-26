using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class SelectComponent : NetworkBehaviour
{ 
    private readonly UnityEvent _onSelect = null;
    private readonly UnityEvent _onDeselect = null;
    
    private MoveComponent _moveComponent;
    private PlayerAbilities _playerAbilities;
    private UIComponent _uiComponent;
    
    private bool _isCurrentPLayer;

    public Vector3 OffsetInGroup { get; set; }

    public bool IsCurrentPlayer
    {
        get => _isCurrentPLayer;
        set
        {
            _isCurrentPLayer = value;
            if(_isCurrentPLayer) _playerAbilities.SetAbilitiesPanelEnable();
        }
    }

    public void Initialize(MoveComponent move, PlayerAbilities abilities,UIComponent uiComponent)
    {
        _moveComponent = move;
        _playerAbilities = abilities;
        _uiComponent = uiComponent;
    } 
    
    [Client] 
    public void Select()
    {
        if(!isOwned) return;
        
        _uiComponent.ChangeSelection(true);
        _playerAbilities.SetAbilitiesPanelSelect(true);
        _moveComponent.SetOffset(OffsetInGroup);
        _moveComponent.IsSelect = true;
        _moveComponent.UpdatePriority(1f);
        
        _onSelect?.Invoke();
    }
    [Client]
    public void Deselect()
    {
        if(!isOwned) return;
        
        _uiComponent.ChangeSelection(false);
        _playerAbilities.SetAbilitiesPanelSelect(false);
        _moveComponent.SetOffset(OffsetInGroup);
        _moveComponent.IsSelect = false;
        _moveComponent.UpdatePriority(0.5f);
        
        _onDeselect?.Invoke();
    }
}