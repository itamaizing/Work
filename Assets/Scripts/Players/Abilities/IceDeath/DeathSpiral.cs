using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathSpiral : Ability
{
	[SerializeField] private DeathSpiralProjectile _projectile;
	[SerializeField] private Character _playerLinks;
	[SerializeField] private SeriesOfStrikes _seriesOfStrikes;

	private Vector2 _mousePos;

	protected override void Cancel()
	{
		//turn off targets and etc		
	}
	protected override void Cast()
	{
		//PayCost();
		if (_playerLinks.RuneComponent.RemoveRune(2, this))
		{
			_mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			Vector2 lookDir = _mousePos - _playerLinks.Rb.position;
			float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
			_seriesOfStrikes.MakeHit(null, AbilityForm.Magic, 1);
			Shoot(angle);
		}
	}

	[Command]
	private void Shoot(float angle)
	{		
		DeathSpiralProjectile projectile = Instantiate(_projectile, gameObject.transform.position, Quaternion.Euler(0, 0, angle));
		projectile.Init(_playerLinks, 0, false);

		NetworkServer.Spawn(projectile.gameObject);

		RpcInit(projectile.gameObject);
	}

	[ClientRpc]
	private void RpcInit(GameObject obj)
	{
		obj.GetComponent<DeathSpiralProjectile>().Init(_playerLinks, 0, false);
	}

	public void AddCharge()
	{
		_currentChargers++;
	}
}
