using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuneComponent : StaminaComponent
{
	private Ability _lastUsedAbility = null;
	private int _multiplier = 1;
	private float _timer = 0;
	private bool _multiplyCost = false;
	
	private void Update()
	{
		Regen();
		if (!_multiplyCost) return;

		_timer += Time.deltaTime;
		if (_timer > _regenerationDelay)
		{
			_timer = 0;
			_multiplyCost = false;
			_multiplier = 1;
			_lastUsedAbility = null;
		}
	}

	public override void Add(float runeValue)
	{
		_value += runeValue;
		if (_value > _maxValue)
		{
			_value = _maxValue;
		}
		UpdateBar();
	}
	
	public bool RemoveRune(float runeValue, Ability usedAbility) 
	{
		if(_lastUsedAbility == usedAbility && _value >= runeValue*_multiplier * 2)
		{
			_multiplier *= 2;
		}
		runeValue *= _multiplier;
		if(_value >= runeValue)
		{
			_lastUsedAbility = usedAbility;
			_value -= runeValue;
			UpdateBar();
			_multiplyCost = true;
			_timer = 0;
			return true;
		}
		else
		{
			return false;
		}
	}

	public override bool Use(float EnergyValue)
	{
		Debug.Log("ERROR!!! You are using Rune instead of Mana or Energy!!!!");
		return false;
	}
}
