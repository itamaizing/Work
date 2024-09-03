using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuneComponent : Resource
{
	//private Ability _lastUsedAbility = null;
	//private int _multiplier = 1;
	//private float _timer = 0;
	//private bool _multiplyCost = false;

	private List<AbilityTimer> _abilities = new List<AbilityTimer>();
	private bool _disableMultiplier = false;
	private float _lastUsedRuneValue = 0;
	/*private void Update()
	{
		Timer();
	}*/
	public bool RemoveRune(float runeValue, Skill usedAbility) 
	{
		if(_abilities.Count > 0)
		{			
			for(int i = 0; i < _abilities.Count; i++)
			{
				if (_disableMultiplier && CurrentValue >= runeValue * _abilities[i].multiplier && _abilities[i].ability == usedAbility)
				{
					CurrentValue -= runeValue * _abilities[i].multiplier;
					//Debug.Log("Used rune " + runeValue * _skills[i].multiplier);
					_lastUsedRuneValue = runeValue * _abilities[i].multiplier;
					_disableMultiplier = false;
					return true;
				}
				if (_abilities[i].ability == usedAbility && CurrentValue >= runeValue * _abilities[i].multiplier * 2)
				{
					_abilities[i].multiplier *=2;

					runeValue *= _abilities[i].multiplier;

					CurrentValue -= runeValue;
					//Debug.Log("Used rune " + runeValue * _skills[i].multiplier);
					//_multiplyCost = true;
					_abilities[i].time = 6;
					_lastUsedRuneValue = runeValue;
					return true;
				}				
			}
			return false;
		}
		else
		{
			if(CurrentValue >= runeValue)
			{
				AbilityTimer abilityTimer = new AbilityTimer();
				abilityTimer.time = 6;
				abilityTimer.multiplier = 1;
				abilityTimer.ability = usedAbility;
				_abilities.Add(abilityTimer);
				_disableMultiplier = false;
				CurrentValue -= runeValue;
				_lastUsedRuneValue = runeValue;
				return true;
			}
			else 
			{
				return false;
			}
		}
	}

	/*public override bool TryUse(float EnergyValue)
	{
		Debug.LogError("ERROR!!! You are using Rune instead of Mana or Energy!!!!");
		return false;
	}*/

	public void SwitchMultiplier(bool value)
	{
		_disableMultiplier = value;
	}

	private void Timer()
	{
		if(_abilities != null)
		
		for(int i = _abilities.Count - 1; i >= 0; i--) 
			{
				_abilities[i].time -= Time.deltaTime;
				if (_abilities[i].time <= 0)
				{
					_abilities.Remove(_abilities[i]);
				}
			}
		/*foreach (var ability in _skills) 
		{
			ability.time-=Time.deltaTime;
			if(ability.time <= 0 )
			{
				_skills.Remove(ability);
			}
		}*/
	}
	public void IceCloudBonus()
	{
		CurrentValue += _lastUsedRuneValue;
		_lastUsedRuneValue = 0;
	}
}

public class AbilityTimer
{
	public Skill ability;
	public float time;
	public float multiplier;
}
