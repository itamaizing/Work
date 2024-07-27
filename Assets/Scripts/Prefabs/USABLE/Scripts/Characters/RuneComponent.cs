using System.Collections.Generic;
using UnityEngine;

public class RuneComponent : StaminaComponent
{
	private List<AbilityTimer> _abilities = new List<AbilityTimer>();
	private bool _disableMultiplier = false;
	private void Update()
	{
		Regen();
		Timer();
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
				if (_disableMultiplier && _value >= runeValue * _abilities[i].multiplier)
				{
					_value -= runeValue * _abilities[i].multiplier;
					UpdateBar();
					_disableMultiplier = false;
					return true;
				}
				if (_abilities[i].ability == usedAbility && _value >= runeValue * _abilities[i].multiplier * 2)
				{
					_abilities[i].multiplier *= 2;

					runeValue *= _abilities[i].multiplier;

					_value -= runeValue;
					UpdateBar();
					
					_abilities[i].time = 6;
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
				_disableMultiplier = false;
				_value -= runeValue;
				UpdateBar();
				return true;
			}
			else 
			{
				return false;
			}
		}
	}

	public override bool Use(float EnergyValue)
	{
		Debug.LogError("ERROR!!! You are using Rune instead of Mana or Energy!!!!");
		return false;
	}

	public void SwitchMultiplier(bool value)
	{
		_disableMultiplier = value;
	}

	private void Timer()
	{
		if(_abilities !=null) 
			foreach (var ability in _abilities) 
			{ 
				ability.time -= Time.deltaTime; 
				if(ability.time <= 0 ) 
				{ 
					_abilities.Remove(ability); 
				} 
			}
	}
}

public class AbilityTimer
{
	public Ability ability;
	public float time;
	public float multiplier;
}
