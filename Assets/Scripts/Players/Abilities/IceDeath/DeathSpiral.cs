using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathSpiral : Ability
{
	[SerializeField] private DeathSpiralProjectile _projectile;
	[SerializeField] private PlayerLinks _playerLinks;
	//[SerializeField] private RunePlayer _rune;
	//[SerializeField] private Rigidbody2D _rb;

	private Vector2 _mousePos;

	protected override void Cancel()
	{
		//turn off targets and etc		
	}
	protected override void Cast()
	{
		PayCost();
		Shoot();

	}
	private void Shoot()
	{
		_mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		Vector2 lookDir = _mousePos - _playerLinks.Rb.position;
		float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
		DeathSpiralProjectile projectile = Instantiate(_projectile, gameObject.transform.position, Quaternion.Euler(0, 0, angle));
		//projectile.dad = _rb.gameObject;
	}

	protected override void PayCost()
	{
	//руны тоже
		if (Mana.Value >= _manaCost && _isReady)
		{
			Mana.Use(_manaCost);
		}
		else
		{
			TryCancel();
			return;
		}
		_isReady = false;
		//_cooldownJob = StartCoroutine(CooldownCoroutine());
	}

	public void AddCharge()
	{
		_currentChargers++;
	}
}
