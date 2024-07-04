using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuneComponent : StaminaComponent
{
	//private Ability _lastUsedAbility = null;
	//private int _multiplier = 1;
	//private float _timer = 0;
	//private bool _multiplyCost = false;

	private List<AbilityTimer> _abilities;
	private void Update()
	{
		Regen();
		//if (!_multiplyCost) return;

		/*_timer += Time.deltaTime;
		if (_timer > _regenerationDelay)
		{
			_timer = 0;
			_multiplyCost = false;
			_multiplier = 1;
			_lastUsedAbility = null;
		}*/
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
		if(_abilities.Count > 0)
		{
			for(int i = 0; i < _abilities.Count; i++)
			{
				if (_abilities[i].ability == usedAbility && _value >= runeValue * _abilities[i].multiplier * 2)
				{
					var newValue = _abilities[i];
					newValue.multiplier *= 2;
					_abilities[i] = newValue;

					runeValue *= _abilities[i].multiplier;

					_value -= runeValue;
					UpdateBar();
					//_multiplyCost = true;

					var newTimer = _abilities[i];
					newTimer.time = 6;
					_abilities[i] = newTimer;
					return true;
				}				
			}
			return false;
		}
		else
		{
			if(_value >= runeValue)
			{
				AbilityTimer abilityTimer = new AbilityTimer();
				abilityTimer.time = 6;
				abilityTimer.multiplier = 1;
				abilityTimer.ability = usedAbility;
				_abilities.Add(abilityTimer);

				_value -= runeValue;
				UpdateBar();
				return true;
			}
			else 
			{
				return false;
			}
		}

		/*if(_lastUsedAbility == usedAbility && _value >= runeValue*_multiplier * 2)
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
		}*/
	}

	public override bool Use(float EnergyValue)
	{
		Debug.LogError("ERROR!!! You are using Rune instead of Mana or Energy!!!!");
		return false;
	}
}

public struct AbilityTimer
{
	public Ability ability;
	public float time;
	public float multiplier;
}
