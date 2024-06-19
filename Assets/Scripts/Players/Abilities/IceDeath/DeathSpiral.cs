using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathSpiral : Ability
{
	[SerializeField] private DeathSpiralProjectile _projectile;
	[SerializeField] private Character _playerLinks;
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
		if (_playerLinks.RuneComponent.RemoveRune(2, this))
		{
			_mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			Vector2 lookDir = _mousePos - _playerLinks.Rb.position;
			float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
			Shoot(angle);
		}
	}

	[Command]
	private void Shoot(float angle)
	{		
		DeathSpiralProjectile projectile = Instantiate(_projectile, gameObject.transform.position, Quaternion.Euler(0, 0, angle));
		projectile.dad = _playerLinks.Rb.gameObject;

		NetworkServer.Spawn(projectile.gameObject);
	}

	public void AddCharge()
	{
		_currentChargers++;
	}
}
