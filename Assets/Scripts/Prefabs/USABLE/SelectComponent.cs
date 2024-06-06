using UnityEngine;

public class SelectComponent : MonoBehaviour
{
    private MoveComponent _moveComponent;
    private PlayerAbilities _playerAbilities;
    private UIPlayerComponents _uiPlayerComponents;
    private bool isSelect;
    private bool isCurrentPLayer;

    public bool IsCurrentPlayer
    {
        get => isCurrentPLayer;
        set
        {
            isCurrentPLayer = value;
            _playerAbilities.SetAbilitiesPanelEnable(isCurrentPLayer);
        }
        
    }
    public bool IsSelect
    {
        get => isSelect;
        set
        {
            isSelect = value;
            _moveComponent.CanMove = isSelect;
            _uiPlayerComponents.ChangeSelection(isSelect);
            if(isSelect==false) _playerAbilities.SetAbilitiesPanelEnable(false);
        }
    }

    public void Initialize(bool isSelected , MoveComponent move, PlayerAbilities abilities,UIPlayerComponents uiComponents)
    {
        _moveComponent = move;
        _playerAbilities = abilities;
        _uiPlayerComponents = uiComponents;
        IsSelect = isSelected;
        IsCurrentPlayer = isSelected;
        Debug.Log(isSelected);
    }
}
