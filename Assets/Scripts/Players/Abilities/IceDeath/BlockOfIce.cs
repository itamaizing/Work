using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class BlockOfIce : Ability
{
	[SerializeField] private BlockOfIceProjectile _iceArrow;
	[SerializeField] private Character _playerLinks;
	[SerializeField] private SeriesOfStrikes _seriesOfStrikes;
	[SerializeField] private float _castTime = 2.5f;
	private bool _canCast = true;
	private Vector2 _mousePos;
	private bool _enabled;
	
	private void Update()
	{
		if (!_enabled) return;

		if (Input.GetMouseButtonDown(0))
		{
			//PayCost();
			StartCoroutine(Casting());
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
		Debug.Log("shot");
		_mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		Vector2 lookDir = _mousePos - _playerLinks.Rb.position;
		float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
		CmdCreateProjecttile(angle);
		_seriesOfStrikes.MakeHit(null, AbilityForm.Magic, 1);
		//_playerLinks.Stamina.Use(_playerLinks.Stamina.Value);
		Cancel();
	}
	[Command]
	private void CmdCreateProjecttile(float angle)
	{
		BlockOfIceProjectile projectile = Instantiate(_iceArrow, gameObject.transform.position, Quaternion.Euler(0, 0, angle));
		projectile.Init(_playerLinks, _playerLinks.Stamina.Value, false);

		NetworkServer.Spawn(projectile.gameObject);

		RpcInit(projectile.gameObject, _playerLinks.Stamina.Value);
	}

	[ClientRpc]
	private void RpcInit(GameObject obj, float manaValue)
	{
		obj.GetComponent<BlockOfIceProjectile>().Init(_playerLinks, manaValue, false);
	}

	private IEnumerator Casting()
	{
		yield return new WaitForSeconds(_castTime);
		if (_canCast && _playerLinks.RuneComponent.RemoveRune(1, this))
		{
			Shoot();
		}
		else
		{
			Cancel();
		}
	}
}
