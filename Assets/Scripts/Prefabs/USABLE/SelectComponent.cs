using System.Collections.Generic;
using UnityEngine;

public class SelectComponent : MonoBehaviour
{
    private MoveComponent _moveComponent;
    private PlayerAbilities _playerAbilities;
    private UIPlayerComponents _uiPlayerComponents;
    private bool isSelect = false;
    private bool isCurrentPLayer;

    public int NumberInGroup { get; set; }

    public bool IsCurrentPlayer
    {
        get => isCurrentPLayer;
        set
        {
            isCurrentPLayer = value;
            if(isCurrentPLayer) _playerAbilities.SetAbilitiesPanelEnable();
        }
        
    }
    public bool IsSelect
    {
        get => isSelect;
        set
        {
            isSelect = value;
            _uiPlayerComponents.ChangeSelection(isSelect);
            _playerAbilities.SetAbilitiesPanelSelect(isSelect);
            _moveComponent.SetOffset(Positions.unitInGroupPositions[NumberInGroup]);
            _moveComponent.IsSelect = isSelect;
        }
    }

    public void Initialize(bool isSelected , MoveComponent move, PlayerAbilities abilities,UIPlayerComponents uiComponents)
    {
        _moveComponent = move;
        _playerAbilities = abilities;
        _uiPlayerComponents = uiComponents;
        IsSelect = isSelected;
        IsCurrentPlayer = isSelected;
    }
    
}