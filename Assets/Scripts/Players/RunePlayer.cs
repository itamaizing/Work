using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RunePlayer : PlayerStamina
{
	[SerializeField] private SpriteRenderer _runeSprite;

	private Ability _lastUsedAbility = null;
	private float _runeSpriteWidth = 3.7f;
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
		_runeSprite.size = new Vector2(_runeSprite.size.x + _regenerationValue / _maxValue * _runeSpriteWidth, _runeSprite.size.y);
		if(_runeSprite.size.x > _runeSpriteWidth) 
		{
			_runeSprite.size = new Vector2(_runeSpriteWidth, _runeSprite.size.y);
		}
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
			_runeSprite.size = new Vector2(_runeSprite.size.x - runeValue / _maxValue * _runeSpriteWidth, _runeSprite.size.y);
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
		return false;
	}
}
