using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAbilities : MonoBehaviour
{
    [SerializeField] private PlayerMove _playerMove;
    [SerializeField] private ManaPlayer _mana;
    [SerializeField] private HealthPlayer _health;
    [SerializeField] private List<Ability> _abilities;

    private Ability _currentAbility;

    public List<Ability> Abilities => _abilities;

    private void Start()
    {
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
		InputHandler.OnAltClick += CancelSpellCast;

        InputHandler.OnFirstCast += TryUseAbility;
        InputHandler.OnSecondCast += TryUseAbility;
        InputHandler.OnThirdCast += TryUseAbility;
        InputHandler.OnFourthCast += TryUseAbility;
        InputHandler.OnFifthCast += TryUseAbility;
    }

    private void OnDisable()
	{
		InputHandler.OnAltClick -= CancelSpellCast;

        InputHandler.OnFirstCast -= TryUseAbility;
        InputHandler.OnSecondCast -= TryUseAbility;
        InputHandler.OnThirdCast -= TryUseAbility;
        InputHandler.OnFourthCast -= TryUseAbility;
        InputHandler.OnFifthCast -= TryUseAbility;
    }

    private void TryUseAbility(int index)
    {
        if (index >= _abilities.Count)
            return;

        if(_currentAbility.IsUsed == false && _playerMove.IsSelect)
        {
            _currentAbility = _abilities[index];
            _currentAbility.TryUse();
        }
    }

    private void CancelSpellCast()
    {
        if (_currentAbility != null)
            _currentAbility.TryCancel();
    }
}
