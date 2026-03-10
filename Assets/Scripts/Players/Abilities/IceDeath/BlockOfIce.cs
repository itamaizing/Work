using Mirror;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BlockOfIce : Skill
{
	[SerializeField] private BlockOfIceProjectile _iceArrow;
	[SerializeField] private HeroComponent _playerLinks;
	[SerializeField] private SeriesOfStrikes _seriesOfStrikes;
	private Vector3 _mousePos;
	private Energy _energy;

	protected override bool IsCanCast => IsCanCastCheck();

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;

    private bool IsCanCastCheck()
	{
		return true;
	}

	private void Start()
	{
        //_energy = (Energy)_playerLinks.Resources[ResourceType.Energy];
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
		Debug.LogError("data error");
    }

    private void Shoot()
	{
		Debug.Log("shot");
		Vector3 lookDir = _mousePos - _playerLinks.transform.position;
		float angle = Mathf.Atan2(lookDir.z, lookDir.x) * Mathf.Rad2Deg - 90f;
		Debug.Log(angle + " angle");
		CmdCreateProjecttile(angle);
		_seriesOfStrikes.MakeHit(null, AbilityForm.Magic, 1, 0, 0);
	}

	[Command]
	private void CmdCreateProjecttile(float angle)
	{
		BlockOfIceProjectile projectile = Instantiate(_iceArrow, gameObject.transform.position, Quaternion.Euler(0, -angle, 0));
		SceneManager.MoveGameObjectToScene(projectile.gameObject, _hero.NetworkSettings.MyRoom);
		projectile.Init(_playerLinks, _energy.CurrentValue, false, this);

		NetworkServer.Spawn(projectile.gameObject);

		RpcInit(projectile.gameObject, _energy.CurrentValue);
	}

	[ClientRpc]
	private void RpcInit(GameObject obj, float manaValue)
	{
		obj.GetComponent<BlockOfIceProjectile>().Init(_playerLinks, manaValue, false, this);
	}

	protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
	{
		if (_energy == null)
			_energy = (Energy)Hero.Resources[ResourceType.Energy];

        while (float.IsPositiveInfinity(_mousePos.x))
		{			
			if (GetMouseButton)
			{
				_mousePos = Targeting.GetMousePoint();
				/*if (Targeting.GetTarget().isCharater)
				{
					Debug.Log("Character try");
					if (Targeting.GetTarget()?.Character != null)
					{
						//Debug.Log("Character");
						_mousePos = Targeting.GetTarget().Character.transform.position;
						Debug.Log(Vector3.Distance(_mousePos, transform.position) + " Distance");
						if(Vector3.Distance(_mousePos, transform.position) < 0.2f)
						{
							_mousePos = Vector2.positiveInfinity;
						}
					}
				}
				else
				{
					Debug.Log("Position");
					_mousePos = Targeting.GetMousePoint();
				}*/
			}
			yield return null;
		}
		Debug.LogError("Error data");
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
