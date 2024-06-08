using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class PlayerAbilities : MonoBehaviour
{
    [SerializeField] private List<Ability> _abilities;
    [SerializeField] private VisualRender visualRender;

    private Ability _currentAbility;
    private int _currentAbilityIndex;
    private bool _isAbilitiesEnabled = true;

    private AbilityPanel _abilityPanel;

    public List<Ability> Abilities => _abilities;

    public event UnityAction<int> AbilitySelected;
    public event UnityAction<int> AbilityDeselected;

    public void Initialize(MoveComponent playerMove,StaminaComponent staminaComponent , HealthComponent healthComponent)
    {
        if(_abilities.Count > 0)
        {
            _currentAbility = null;
        }
        foreach (var item in _abilities)
        {
            item.SetPlayer(playerMove, staminaComponent, healthComponent);
        }
        _abilityPanel = AbilitiesManager.Instance.AddPanel(this);
    }

    private void EnableAbilities()
	{
        InputHandler.OnClick += TryUseAbility;
        InputHandler.OnAltClick += CancelSpellCast;

        InputHandler.OnFirstCast += SetCurrentAbility;
        InputHandler.OnSecondCast += SetCurrentAbility;
        InputHandler.OnThirdCast += SetCurrentAbility;
        InputHandler.OnFourthCast += SetCurrentAbility;
        InputHandler.OnFifthCast += SetCurrentAbility;
        InputHandler.OnSixthCast += SetCurrentAbility;
        InputHandler.OnSeventhCast += SetCurrentAbility;
        InputHandler.OnEighthCast += SetCurrentAbility;
    }

    private void DisableAbilities()
	{
        InputHandler.OnClick -= TryUseAbility;
        InputHandler.OnAltClick -= CancelSpellCast;

        InputHandler.OnFirstCast -= SetCurrentAbility;
        InputHandler.OnSecondCast -= SetCurrentAbility;
        InputHandler.OnThirdCast -= SetCurrentAbility;
        InputHandler.OnFourthCast -= SetCurrentAbility;
        InputHandler.OnFifthCast -= SetCurrentAbility;
        InputHandler.OnSixthCast -= SetCurrentAbility;
        InputHandler.OnSeventhCast -= SetCurrentAbility;
        InputHandler.OnEighthCast -= SetCurrentAbility;
    }

    public void SetAbilitiesEnable(bool isEnabled)
    {
        _isAbilitiesEnabled = isEnabled;
    }

    public void SetAbilitiesPanelSelect(bool isSelect)
    {
        AbilitiesManager.Instance.ChangeCurrentPanelSelectStatus(_abilityPanel,isSelect);
        if(isSelect) EnableAbilities();
        else DisableAbilities();
    }

    public void SetAbilitiesPanelEnable()
    {
        AbilitiesManager.Instance.ActiveCurrentPanel(_abilityPanel);
    }

    private void SetCurrentAbility(int index)
    {
        if (index >= _abilities.Count)
            return;

        if (_currentAbility == null)
        {
            _currentAbilityIndex = index;
            AbilitySelected?.Invoke(index);

            _currentAbility = _abilities[index];
            _currentAbility.PreparingEnded += visualRender.StopDraw;
            _currentAbility.Cancled += visualRender.StopDraw;
            _currentAbility.AreaOffed += visualRender.StopAreaDraw;

            TryUseAbility();
        }
        else if (!_currentAbility.IsUsed)
        {
            AbilityDeselected?.Invoke(_currentAbilityIndex);
            _currentAbilityIndex = index;
            AbilitySelected?.Invoke(index);

            _currentAbility.PreparingEnded -= visualRender.StopDraw;
            _currentAbility.Cancled -= visualRender.StopDraw;
            _currentAbility.AreaOffed -= visualRender.StopAreaDraw;
            _currentAbility = _abilities[index];
            _currentAbility.PreparingEnded += visualRender.StopDraw;
            _currentAbility.Cancled += visualRender.StopDraw;
            _currentAbility.AreaOffed += visualRender.StopAreaDraw;

            TryUseAbility();
        }
    }

    private void TryUseAbility()
    {
        if (_currentAbility == null || !_isAbilitiesEnabled || !_abilityPanel.IsActive  || _currentAbility.IsUsed )
            return;

        visualRender.Drawn(_currentAbility);
        _currentAbility.TryUse();
    }

    private void CancelSpellCast()
    {
        if (_currentAbility != null)
        {
            if(_currentAbility.IsUsed)
            {
                _currentAbility.TryCancel();
            }
            else
            {
                _currentAbility = null;
                AbilityDeselected?.Invoke(_currentAbilityIndex);
            }
        }
    }

    private void OnDestroy()
    {
        AbilitiesManager.Instance.RemovePanel(_abilityPanel);
    }
}
