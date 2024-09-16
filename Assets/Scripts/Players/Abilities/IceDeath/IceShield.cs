using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class IceShield : Skill
{
	[SerializeField] private float _percentOfShield = 0.9f;
	[SerializeField] private float _decreaseSpeed = 0.2f;
	[SerializeField] private HeroComponent _playerLinks;
	[SerializeField] private SeriesOfStrikes _combo;

	private IceShieldObj _shield;
	private bool _active = false;
	private float _timer = 1f;
	private float _delay = 1f;
	private Energy _energy;

	protected override bool IsCanCast => throw new System.NotImplementedException();

	private void Start()
	{
		for (int i = 0; i < _playerLinks.Resources.Count; i++)
		{
			if (_playerLinks.Resources[i].Type == ResourceType.Energy)
			{
				_energy = (Energy)_playerLinks.Resources[i];
			}
		}

	}

	private void Update()
	{
		Timer();
	}

	private void Shoot() 
	{
		_active = !_active;

		if (_active) 
		{
			_playerLinks.Move.ChangeMoveSpeed(0.8f);
			//IceShieldObj shield = new IceShieldObj(Health, _playerLinks.Stamina.CurrentValue, DamageType.Both);
			//_shield = shield;
			//create shield
			//_character.Health.
		}
		else
		{
			_playerLinks.Move.ChangeMoveSpeed(1.25f);
			//Health.RemoveShield(_shield, DamageType.Both);
			_shield = null;
		}
	}

	private void Timer()
	{
		if (_active)
		{
			_timer -= Time.deltaTime;
			if (_timer > 0) return;

		/*	if (_character.Stamina.TryUse(1))
			{
				_timer = _delay;
			}
			else
			{
				_active = false;
			}
		*/
		}
	}

	protected override IEnumerator PrepareJob()
	{
		throw new System.NotImplementedException();
	}

	protected override IEnumerator CastJob()
	{
		throw new System.NotImplementedException();
	}

	protected override void ClearData()
	{
		throw new System.NotImplementedException();
	}
}


public class IceShieldObj : Shielding
{
	//private GameObject _enemy;
	public IceShieldObj(HealthComponent healthComponent, float shieldValue, DamageType damageType) : base(healthComponent, shieldValue, damageType)
	{
		//_enemy = enemy;
	}

	protected new void AddShieldBehavior(HealthComponent healthComponent, DamageType damageType)
	{
		healthComponent.AddShieldBehavior(this, damageType);
	}

	public override float GetShieldAmount(GameObject obj)
	{
		if(obj == null)
		{
			return _shieldAmount;
		}

		Vector2 lookDir = obj.transform.position - _healthComponent.transform.position;
		float _angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
		if (_angle > -75 && _angle < 75)
		{
			return _shieldAmount;
		}
		else
		{
			return 0;
		}
	}

	public override void RemoveAmount(float amount)
	{
		_shieldAmount -= amount;
	}
}
