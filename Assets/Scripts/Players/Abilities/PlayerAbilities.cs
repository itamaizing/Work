using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerAbilities : MonoBehaviour
{
    [SerializeField] private PlayerMove _playerMove;
    [SerializeField] private PlayerStamina _mana;
    [SerializeField] private HealthPlayer _health;
    [SerializeField] private List<Ability> _abilities;
    [SerializeField] private AbilityRender _abilityRender;

    [SerializeField] private Ability _currentAbility;
    private int _currentAbilityIndex;
    private bool _isAbilitiesDisabled = false;

    public List<Ability> Abilities => _abilities;

    public event UnityAction<int> AbilitySelected;
    public event UnityAction<int> AbilityDeselected;

    private void Start()
    {
        _playerMove.Selected += OnSelected;
        _playerMove.Deselected += OnDeselected;

        if(_abilities.Count > 0)
        {
            _currentAbility = null;
        }
        foreach (var item in _abilities)
        {
            item.SetPlayer(_playerMove, _mana, _health);
        }
    }

    private void OnEnable()
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

    private void OnDisable()
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

    public void SetAbilitiesDisabled()
    {
        _isAbilitiesDisabled = true;
    }

    public void SetAbilitiesEnabled()
    {
        _isAbilitiesDisabled = false;
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
            _currentAbility.PreparingEnded += _abilityRender.StopDraw;
            _currentAbility.Cancled += _abilityRender.StopDraw;
            _currentAbility.AreaOffed += _abilityRender.StopAreaDraw;

            TryUseAbility();
        }
        else if (_currentAbility.IsUsed == false)
        {
            AbilityDeselected?.Invoke(_currentAbilityIndex);
            _currentAbilityIndex = index;
            AbilitySelected?.Invoke(index);

            _currentAbility.PreparingEnded -= _abilityRender.StopDraw;
            _currentAbility.Cancled -= _abilityRender.StopDraw;
            _currentAbility.AreaOffed -= _abilityRender.StopAreaDraw;
            _currentAbility = _abilities[index];
            _currentAbility.PreparingEnded += _abilityRender.StopDraw;
            _currentAbility.Cancled += _abilityRender.StopDraw;
            _currentAbility.AreaOffed += _abilityRender.StopAreaDraw;

            TryUseAbility();
        }
    }

    private void TryUseAbility()
    {
        if (_currentAbility == null || _isAbilitiesDisabled == true || _currentAbility.IsUsed == true)
            return;

        _abilityRender.Drawn(_currentAbility);
        _currentAbility.TryUse();
    }

    private void CancelSpellCast()
    {
        if (_currentAbility != null)
        {
            if(_currentAbility.IsUsed == true)
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

    private void OnDeselected()
    {
        this.enabled = false;
    }

    private void OnSelected()
    {
        this.enabled = true;
    }
}
