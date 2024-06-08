using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class PlayerAbilities : MonoBehaviour
{
    [SerializeField] private List<Ability> _abilities;
    [FormerlySerializedAs("_abilityRender")] [SerializeField] private VisualRender visualRender;

    private Ability _currentAbility;
    private int _currentAbilityIndex;
    private bool _isAbilitiesDisabled = false;

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
            Debug.Log("set");
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
        if (_currentAbility == null || _isAbilitiesDisabled  || _currentAbility.IsUsed )
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

    public void SwitchAvaliable(Schools school, bool value)
    {
		if (school == Schools.Physical)
			return;
		foreach (var item in _abilities) 
        {
            if(item.School == school)
            {
                item.SwitchAvailible(value);
                //item.KnockDownTimerStart(coolDown);
            }
        }
    }
	public void SwitchAvaliable(AbilityForm form, bool value)
	{       
		foreach (var item in _abilities)
		{
			if (item.AbilityForm == form)
			{
				item.SwitchAvailible(value);
				//item.KnockDownTimerStart(coolDown);
			}
		}
	}

    private IEnumerator StartKnockDownTimer(float coolDown, Ability ability)
    {
		yield return new WaitForSeconds(coolDown);
        ability.SwitchAvailible(true);
	}
}
