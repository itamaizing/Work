using Mirror;
using UnityEngine;

public class IcyStream : Ability
{
	[SerializeField] private IcyStreamProjectile _projectile;
	[SerializeField] private Character _playerLinks;
	[SerializeField] private FrostingFrozenTalant _frostingFrozenTalant;
	[SerializeField] private SeriesOfStrikes _seriesOfStrikes;
	//[SerializeField] private RunePlayer _rune;
	//[SerializeField] private Rigidbody2D _rb;

	private Vector2 _mousePos;
	private float _angle;
	private bool _enabled;

	private void Update()
	{
		if (!_enabled) return;

		if (Input.GetMouseButtonDown(0))
		{
			//PayCost();
			if (_playerLinks.RuneComponent.RemoveRune(1.5f, this))
			{
				Shoot();
			}
			else
			{
				Cancel();
			}
		}
		if (Input.GetMouseButtonDown(1))
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
		Debug.Log("shot");
		_mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		Vector2 lookDir = _mousePos - _playerLinks.Rb.position;
		float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
		CmdCreateProjecttile(angle);
		float usedEnergy = 0;
		if (_playerLinks.Stamina.Value >= 40)
		{
			usedEnergy = 40;
		}
		else
		{
			usedEnergy = _playerLinks.Stamina.Value;
		}
		_playerLinks.Stamina.Use(usedEnergy);
		_seriesOfStrikes.MakeHit(null, AbilityForm.Magic, 1);
		Cancel();
	}

	[Command]
	private void CmdCreateProjecttile(float angle)
	{
		IcyStreamProjectile projectile = Instantiate(_projectile, gameObject.transform.position, Quaternion.Euler(0, 0, angle));
		projectile.talant = _frostingFrozenTalant;
		projectile.Init(_playerLinks, _playerLinks.Stamina.Value, false);

		NetworkServer.Spawn(projectile.gameObject);

		RpcInit(projectile.gameObject, _playerLinks.Stamina.Value);
	}

	[ClientRpc]
	private void RpcInit(GameObject obj, float manaValue)
	{
		obj.GetComponent<IceCloudProjectile>().Init(_playerLinks, manaValue, false);
	}
}
