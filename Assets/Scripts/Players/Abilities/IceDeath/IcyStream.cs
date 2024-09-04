using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IcyStream : Skill
{
	[SerializeField] private IcyStreamProjectile _projectile;
	[SerializeField] private Character _playerLinks;
	[SerializeField] private SeriesOfStrikes _seriesOfStrikes;
	private bool _talent = false;

	private Vector2 _mousePos = Vector3.positiveInfinity;
	protected override bool IsCanCast => true;


	private void Shoot()
	{
		Debug.Log("shot");
		_mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		Vector2 lookDir = _mousePos - _playerLinks.Rb.position;
		float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
		CmdCreateProjecttile(angle);
		float usedEnergy = 0;
		if (_playerLinks.Stamina.CurrentValue >= 40)
		{
			usedEnergy = 40;
		}
		else
		{
			usedEnergy = _playerLinks.Stamina.CurrentValue;
		}
		_playerLinks.Stamina.TryUse(usedEnergy);
		_seriesOfStrikes.MakeHit(null, AbilityForm.Magic, 1, 1);
	}

	[Command]
	private void CmdCreateProjecttile(float angle)
	{
		IcyStreamProjectile projectile = Instantiate(_projectile, gameObject.transform.position, Quaternion.Euler(0, 0, angle));
		SceneManager.MoveGameObjectToScene(projectile.gameObject, _hero.NetworkSettings.MyRoom);
		projectile.Init(_playerLinks, _playerLinks.Stamina.CurrentValue, _talent); //its talent bool, no last hit 

		NetworkServer.Spawn(projectile.gameObject);

		RpcInit(projectile.gameObject, _playerLinks.Stamina.CurrentValue);
	}

	[ClientRpc]
	private void RpcInit(GameObject obj, float manaValue)
	{
		obj.GetComponent<IceCloudProjectile>().Init(_playerLinks, manaValue, false);
	}

	protected override IEnumerator PrepareJob()
	{
		while (float.IsPositiveInfinity(_mousePos.x))
		{
			if (Input.GetMouseButton(0))
			{
				_playerLinks.RuneComponent.CmdUse(1.5f);
				_mousePos = GetMousePoint();
			}
			yield return null;
		}
	}

	protected override IEnumerator CastJob()
	{
		Shoot();
		yield return null;
	}

	protected override void ClearData()
	{
		_mousePos = Vector3.positiveInfinity;
	}

	public void Talent(bool value)
	{
		_talent = value;
	}
}
