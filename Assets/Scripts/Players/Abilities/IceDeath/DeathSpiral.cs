using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathSpiral : Ability
{
	[SerializeField] private DeathSpiralProjectile _projectile;
	[SerializeField] private Character _playerLinks;
	[SerializeField] private SeriesOfStrikes _seriesOfStrikes;

	private float _timer = 1f;
	private Vector2 _mousePos;
	private bool _inTheRow = false;

	private void Update()
	{
		Timer();
	}

	protected override void Cancel()
	{
		//turn off targets and etc		
	}
	protected override void Cast()
	{
		//PayCost();
		if(_inTheRow) 
		{
			_mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			Vector2 lookDir = _mousePos - _playerLinks.Rigidbody2D.position;
			float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
			_seriesOfStrikes.MakeHit(null, AbilityForm.Magic, 1);
			Shoot(angle);
		}
		else if (_playerLinks.RuneComponent.RemoveRune(2, this))
		{
			_currentChargers--;
			_inTheRow = true;
			_mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			Vector2 lookDir = _mousePos - _playerLinks.Rigidbody2D.position;
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
		if(_currentChargers<_maxCharges)
			_currentChargers++;
	}

	private void Timer()
	{
		if (!_inTheRow) return;
		_timer-= Time.deltaTime;
		if(_timer <= 0)
		{
			_inTheRow = false;
			_timer = 1; 
		}
	}
}
