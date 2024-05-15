using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAbilities : MonoBehaviour
{
    [SerializeField] private PlayerMove _playerMove;
    [SerializeField] private PlayerStamina _mana;
    [SerializeField] private HealthPlayer _health;
    [SerializeField] private List<Ability> _abilities;
    [SerializeField] private AbilityRender _abilityRender;

    private Ability _currentAbility;
    private bool _isAbilitiesDisabled = false;

    public List<Ability> Abilities => _abilities;

    private void Start()
    {
        _playerMove.Selected += OnSelected;
        _playerMove.Deselected += OnDeselected;

        if(_abilities.Count > 0)
        {
            _currentAbility = _abilities[0];
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
        
        if(_currentAbility.IsUsed == false)
        {
            _currentAbility.PreparingEnded -= _abilityRender.StopDraw;
            _currentAbility = _abilities[index];
            _currentAbility.PreparingEnded += _abilityRender.StopDraw;
            TryUseAbility();
        }
    }

    private void TryUseAbility()
    {
        if (_isAbilitiesDisabled == true || _currentAbility.IsUsed == true)
            return;

        _abilityRender.Drawn(_currentAbility);
        _currentAbility.TryUse();
    }

    private void CancelSpellCast()
    {
        if (_currentAbility != null)
        {
            _currentAbility.TryCancel();
            _abilityRender.StopDraw();
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
