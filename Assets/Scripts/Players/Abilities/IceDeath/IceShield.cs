using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IceShield : Ability
{
	[SerializeField] private float _percentOfShield = 0.9f;
	[SerializeField] private float _decreaseSpeed = 0.2f;
	[SerializeField] private Character _character;
	[SerializeField] private SeriesOfStrikes _combo;

	private bool _active = false;
	private float _timer = 1f;
	private float _delay = 1f;

	private void Update()
	{
		Timer();
	}
	protected override void Cast()
	{
		PayCost();
		if (_character.RuneComponent.RemoveRune(1, this))
		{
			Shoot();
		}
		else
		{
			TryCancel();
		}
	}

	protected override void Cancel()
	{
		
	}

	private void Shoot() 
	{
		_active = !_active;

		if (_active) 
		{
			_character.Move.ChangeMoveSpeed(0.8f);
			//create shield
			//_character.Health.
		}
		else
		{
			_character.Move.ChangeMoveSpeed(1.25f);
		}
	}

	private void Timer()
	{
		if (_active)
		{
			_timer -= Time.deltaTime;
			if (_timer > 0) return;

			if (_character.Stamina.Use(1))
			{
				_timer = _delay;
			}
			else
			{
				_active = false;
			}

		}
	}
}


public class IceShieldObj : Shielding
{
	public IceShieldObj(HealthComponent healthComponent, float shieldValue, DamageType damageType) : base(healthComponent, shieldValue, damageType)
	{
		
	}

	protected new void AddShieldBehavior(HealthComponent healthComponent, DamageType damageType)
	{
		healthComponent.AddShieldBehavior(this, damageType);
	}
}
