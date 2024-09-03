using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Icecloud : Skill
{
	[SerializeField] private IceCloudProjectile _projectile;
	[SerializeField] private Character _playerLinks;
	[SerializeField] private SeriesOfStrikes _combo;

	private Vector2 _mousePos = Vector3.positiveInfinity;
	//private bool _enabled;
	private bool _boostDmg;

	protected override bool IsCanCast => true;
	
	private void Shoot()
	{
		Buff.AttackSpeed.ReductionPercentage(1 + _combo.GetMultipliedSpeed() / 100);

		//_playerLinks.RuneComponent.SwitchMultiplier(true);
		//_mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		Vector2 lookDir = _mousePos - _playerLinks.Rigidbody2D.position;
		float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
		if( _combo.MakeHit(null, AbilityForm.Magic, 1, 0))
		{
			_playerLinks.RuneComponent.IceCloudBonus();
		}

		Buff.AttackSpeed.IncreasePercentage(1 + _combo.GetMultipliedSpeed() / 100);

		CmdCreateProjecttile(angle, _playerLinks.Stamina.CurrentValue);
		_playerLinks.Stamina.TryUse(_playerLinks.Stamina.CurrentValue);
		ClearData();
	}

	[Command]
	private void CmdCreateProjecttile(float angle, float manaValue)
	{
		IceCloudProjectile projectile = Instantiate(_projectile, gameObject.transform.position, Quaternion.Euler(0, 0, angle));
		SceneManager.MoveGameObjectToScene(projectile.gameObject, _hero.NetworkSettings.MyRoom);
		projectile.Init(_playerLinks, manaValue, false);		

		NetworkServer.Spawn(projectile.gameObject);

		RpcInit(projectile.gameObject, manaValue);
	}

	[ClientRpc]
	private void RpcInit(GameObject obj, float manaValue)
	{
		obj.GetComponent<IceCloudProjectile>().Init(_playerLinks, manaValue, false);
	}

	public void TalentBoostDmg(bool value)
	{
		_boostDmg = value;
	}

	protected override IEnumerator PrepareJob()
	{
		while (float.IsPositiveInfinity(_mousePos.x))
		{
			if (Input.GetMouseButton(0))
			{
				_playerLinks.RuneComponent.CmdUse(1);
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
		_mousePos = Vector2.positiveInfinity;
		//_enabled = false;
	}
}
