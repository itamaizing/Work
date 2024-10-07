using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IceShard : Skill
{
	[SerializeField] private IceShardProjectile _projectile;
	[SerializeField] private HeroComponent _playerLinks;
	[SerializeField] private SeriesOfStrikes _seriesOfStrikes;

	private Vector2 _mousePos = Vector2.positiveInfinity;
	private bool _talentPlague = false;
	private bool _talentChragesPlague = false;
	private Energy _energy;

	protected override bool IsCanCast => true;

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

	private void Shoot()
	{
		_mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		Vector2 lookDir = _mousePos - (Vector2)_playerLinks.transform.position;
		float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
		_seriesOfStrikes.MakeHit(null, AbilityForm.Magic, 1, 3);

		CmdCreateProjecttile(angle, _energy.CurrentValue);
	}

	[Command]
	private void CmdCreateProjecttile(float angle, float manaValue)
	{
		IceShardProjectile projectile = Instantiate(_projectile, gameObject.transform.position, Quaternion.Euler(0, 0, angle));
		SceneManager.MoveGameObjectToScene(projectile.gameObject, _hero.NetworkSettings.MyRoom);
		projectile.Init(_playerLinks, manaValue, false, this);
		projectile.Talents(_talentPlague, _talentChragesPlague);

		NetworkServer.Spawn(projectile.gameObject);

		RpcInit(projectile.gameObject, manaValue);
	}

	[ClientRpc]
	private void RpcInit(GameObject obj, float manaValue)
	{
		obj.GetComponent<IceShardProjectile>().Init(_playerLinks, manaValue, false, this);
	}

	public void TalentPlague(bool value)
	{
		_talentPlague = value;
	}
	public void TalentChargesPlague(bool value)
	{
		_talentChragesPlague = value;
	}

	protected override IEnumerator PrepareJob()
	{
		while (float.IsPositiveInfinity(_mousePos.x))
		{
			if (GetMouseButton)
			{
				_energy.TryUse(5);
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
	}
}
