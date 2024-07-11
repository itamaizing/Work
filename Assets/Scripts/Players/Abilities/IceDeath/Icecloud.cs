using Mirror;
using UnityEngine;

public class Icecloud : Ability
{
	[SerializeField] private IceCloudProjectile _projectile;
	[SerializeField] private Character _playerLinks;
	[SerializeField] private SeriesOfStrikes _combo;

	private Vector2 _mousePos;
	private bool _enabled;
	
	private void Update()
	{
		if (!_enabled) return;
		
		if (Input.GetMouseButtonDown(0))
		{
			PayCost();
			if (_playerLinks.RuneComponent.RemoveRune(1, this))
			{
				Shoot();
			}
			else
			{
				Cancel();
			}
		}
		if(Input.GetMouseButtonDown(1)) 
		{
			Cancel();
		}
	}

	protected override void Cast()
	{
		_enabled = true;
		/*if(_playerLinks.RuneComponent.RemoveRune(1, this)) 
		{
			Shoot();
		}*/
	}

	protected override void Cancel()
	{
		_enabled = false;
	}
	
	private void Shoot()
	{
		Buff.AttackSpeed.ReductionPercentage(1 + _combo.GetMultipliedSpeed() / 100);

		_playerLinks.RuneComponent.SwitchMultiplier(true);
		_mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		Vector2 lookDir = _mousePos - _playerLinks.Rb.position;
		float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
		_combo.MakeHit(null, AbilityForm.Magic, 1);

		Buff.AttackSpeed.IncreasePercentage(1 + _combo.GetMultipliedSpeed() / 100);

		CmdCreateProjecttile(angle, _playerLinks.Stamina.Value);
		_playerLinks.Stamina.Use(_playerLinks.Stamina.Value);
		Cancel();
	}

	[Command]
	private void CmdCreateProjecttile(float angle, float manaValue)
	{
		IceCloudProjectile projectile = Instantiate(_projectile, gameObject.transform.position, Quaternion.Euler(0, 0, angle));
		projectile.Init(_playerLinks, manaValue, false);
		
		NetworkServer.Spawn(projectile.gameObject);

		RpcInit(projectile.gameObject, manaValue);
	}

	[ClientRpc]
	private void RpcInit(GameObject obj, float manaValue)
	{
		obj.GetComponent<IceCloudProjectile>().Init(_playerLinks, manaValue, false);
	}
}
