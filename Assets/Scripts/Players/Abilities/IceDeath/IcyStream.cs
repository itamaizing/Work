using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IcyStream : Skill
{
	[SerializeField] private IcyStreamProjectile _projectile;
	[SerializeField] private HeroComponent _playerLinks;
	[SerializeField] private SeriesOfStrikes _seriesOfStrikes;
	private bool _talent = false;

	private Vector2 _mousePos = Vector3.positiveInfinity;
	private Energy _energy;
	private RuneComponent _rune;

	protected override bool IsCanCast => IsCanCastCheck();

	private bool IsCanCastCheck()
	{
		if (_rune.CurrentValue >= 1.5f)
		{
			_rune.CmdUse(1.5f);
			return true;
		}
		else
		{
			return false;
		}
	}

	private void Start()
	{
		for (int i = 0; i < _playerLinks.Resources.Count; i++)
		{
			if (_playerLinks.Resources[i].Type == ResourceType.Energy)
			{
				_energy = (Energy)_playerLinks.Resources[i];
			}
			if (_playerLinks.Resources[i].Type == ResourceType.Rune)
			{
				_rune = (RuneComponent)_playerLinks.Resources[i];
			}
		}

	}
	private void Shoot()
	{
		Debug.Log("shot");
		_mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		Vector2 lookDir = _mousePos - (Vector2)_playerLinks.transform.position;
		float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
		CmdCreateProjecttile(angle);
		float usedEnergy = 0;
		if (_energy.CurrentValue >= 40)
		{
			usedEnergy = 40;
		}
		else
		{
			usedEnergy = _energy.CurrentValue;
		}
		_energy.TryUse(usedEnergy);
		_seriesOfStrikes.MakeHit(null, AbilityForm.Magic, 1, 1);
	}

	[Command]
	private void CmdCreateProjecttile(float angle)
	{
		IcyStreamProjectile projectile = Instantiate(_projectile, gameObject.transform.position, Quaternion.Euler(0, 0, angle));
		SceneManager.MoveGameObjectToScene(projectile.gameObject, _hero.NetworkSettings.MyRoom);
		projectile.Init(_playerLinks, _energy.CurrentValue, _talent, this); //its talent bool, no last hit 

		NetworkServer.Spawn(projectile.gameObject);

		RpcInit(projectile.gameObject, _energy.CurrentValue);
	}

	[ClientRpc]
	private void RpcInit(GameObject obj, float manaValue)
	{
		obj.GetComponent<IceCloudProjectile>().Init(_playerLinks, manaValue, false, this);
	}

	protected override IEnumerator PrepareJob()
	{
		while (float.IsPositiveInfinity(_mousePos.x))
		{
			if (Input.GetMouseButton(0))
			{
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
