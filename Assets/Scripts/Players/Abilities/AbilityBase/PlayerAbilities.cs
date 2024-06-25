using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class PlayerAbilities : MonoBehaviour
{
    [SerializeField] private List<Ability> _abilities;
    [SerializeField] private VisualRender visualRender;

	private float _globalCooldownTime = 2f;
	private Ability _currentAbility;
	private AutoAttackAbility _currentAutoAttackAbility;
	private int _currentAbilityIndex;
	private int _currentAutoAttackAbilityIndex;
	private bool _isAbilitiesDisabled = false;
	private bool _isAbilitiesEnabled = true;
	private AbilityPanel _abilityPanel;
	private float _abilitySpeedCast = 1;

	public List<Ability> Abilities => _abilities;

    public event UnityAction<int> AbilitySelected;
	public event UnityAction<int> AbilityAutoAttackSelected;
	public event UnityAction<int> AbilityDeselected;
	public event UnityAction<int> AbilityAutoAttackDeselected;

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

    public void SetAbilitiesDisabled()
    {
        _isAbilitiesDisabled = true;
    }

    public void SetAbilitiesEnabled()
    {
        _isAbilitiesDisabled = false;
    }
	public void SetAbilitiesEnable(bool isEnabled)
	{
		_isAbilitiesEnabled = isEnabled;
	}


	public void SetAbilitiesPanelSelect(bool isSelect)
	{
		AbilitiesManager.Instance.ChangeCurrentPanelSelectStatus(_abilityPanel, isSelect);
		if (isSelect) EnableAbilities();
		else DisableAbilities();
	}
	public void SetAbilitiesPanelEnable()
	{
		AbilitiesManager.Instance.ActiveCurrentPanel(_abilityPanel);
	}
	public void SetAbilitiesCoolDown(float time)
	{
		foreach (var item in _abilities)
		{
			item.SetCooldown(time);
		}
	}
	private void SetCurrentAbility(int index)
    {
        if (index >= _abilities.Count)
            return;

        if (_currentAbility == null)
        {
            _currentAbilityIndex = index;
            AbilitySelected?.Invoke(index);

			if (_abilities[index].IsAutoAttack)
			{
				if (_currentAutoAttackAbility != null)
				{
					_currentAutoAttackAbility.TryCancel();
					_currentAutoAttackAbility = null;
				}
				_currentAutoAttackAbility = _abilities[index] as AutoAttackAbility;
				_currentAutoAttackAbilityIndex = index;

				_currentAutoAttackAbility.Cancled += OnAbilityAutoAttackDeselected;
				_currentAutoAttackAbility.CastStarted += OnAbilityAutoAttackSelected;
			}
			_currentAbility = _abilities[index];

			SubscribingAbilityEvents();

			TryUseAbility();
		}
		else if (_currentAbility.IsUsed == false || _currentAbility == _currentAutoAttackAbility)
		{
			AbilityDeselected?.Invoke(_currentAbilityIndex);
			_currentAbilityIndex = index;
			AbilitySelected?.Invoke(index);

			UnsubscribingAbilityEvents();

			if (_abilities[index].IsAutoAttack)
			{
				if (_currentAutoAttackAbility != null)
				{
					_currentAutoAttackAbility.Cancled -= OnAbilityAutoAttackDeselected;
					_currentAutoAttackAbility.CastStarted -= OnAbilityAutoAttackSelected;

					_currentAutoAttackAbility.TryCancel();
					_currentAutoAttackAbility = null;
				}
				_currentAutoAttackAbility = _abilities[index] as AutoAttackAbility;
				_currentAutoAttackAbilityIndex = index;

				_currentAutoAttackAbility.Cancled += OnAbilityAutoAttackDeselected;
				_currentAutoAttackAbility.CastStarted += OnAbilityAutoAttackSelected;
			}
			_currentAbility = _abilities[index];

			SubscribingAbilityEvents();

			TryUseAbility();
		}
	}
	private void SubscribingAbilityEvents()
	{
		_currentAbility.PreparingEnded += visualRender.StopDraw;
		_currentAbility.Cancled += visualRender.StopDraw;
		_currentAbility.AreaOffed += visualRender.StopAreaDraw;
		_currentAbility.CastEnded += GlobalCooldown;

		if (_currentAutoAttackAbility != null && _currentAbility != _currentAutoAttackAbility)
		{
			_currentAbility.CastStarted += PauseAutoAttack;
			_currentAbility.CastEnded += ContinueAutoAttack;
			_currentAbility.Cancled += ContinueAutoAttack;
		}
	}
	private void UnsubscribingAbilityEvents()
	{
		_currentAbility.PreparingEnded -= visualRender.StopDraw;
		_currentAbility.Cancled -= visualRender.StopDraw;
		_currentAbility.AreaOffed -= visualRender.StopAreaDraw;
		_currentAbility.CastEnded -= GlobalCooldown;

		if (_currentAutoAttackAbility != null && _currentAbility != _currentAutoAttackAbility)
		{
			_currentAbility.CastStarted -= PauseAutoAttack;
			_currentAbility.CastEnded -= ContinueAutoAttack;
			_currentAbility.Cancled -= ContinueAutoAttack;
		}
	}
	/*private void TryUseAbility()
    {
        if (_currentAbility == null || _isAbilitiesDisabled  || _currentAbility.IsUsed )
            return;

        visualRender.Drawn(_currentAbility);
        _currentAbility.TryUse();
    }*/
	private void TryUseAbility()
	{
		if (_currentAbility == null || !_isAbilitiesEnabled || !_abilityPanel.IsActive || (_currentAbility.IsUsed))
			return;

		if (_currentAutoAttackAbility != null)
			PauseAutoAttack();

		visualRender.Drawn(_currentAbility);
		_currentAbility.TryUse();
	}
	private void ContinueAutoAttack()
	{
		_currentAutoAttackAbility.Continue();
	}
	private void PauseAutoAttack()
	{
		_currentAutoAttackAbility.Pause();
	}

	private void OnAbilityAutoAttackSelected()
	{
		AbilityAutoAttackSelected?.Invoke(_currentAutoAttackAbilityIndex);
	}

	private void OnAbilityAutoAttackDeselected()
	{
		AbilityAutoAttackDeselected?.Invoke(_currentAutoAttackAbilityIndex);
	}
	private void CancelSpellCast()
    {
		if (_currentAbility != null)
		{
			if (_currentAbility.IsUsed)
			{
				_currentAbility.TryCancel();
			}
			else if (_currentAutoAttackAbility != null && _currentAutoAttackAbility.IsUsed)
			{
				_currentAutoAttackAbility.TryCancel();
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
	private void GlobalCooldown()
	{
		SetAbilitiesCoolDown(_globalCooldownTime);
	}

	private void OnDestroy()
	{
		AbilitiesManager.Instance.RemovePanel(_abilityPanel);
	}
}
