using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Icecloud : Skill
{
	[SerializeField] private IceCloudProjectile _projectile;
	[SerializeField] private HeroComponent _playerLinks;
	[SerializeField] private SeriesOfStrikes _combo;

	private Vector3 _mousePos = Vector3.positiveInfinity;
	
	//private bool _enabled;
	private bool _boostDmg;
	private Energy _energy;
	//private RuneComponent _rune;

	protected override bool IsCanCast => IsCanCastCheck();

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;

    private bool IsCanCastCheck()
	{
		return true;
		/*if (_rune.CurrentValue >= 1)
		{
			_rune.CmdUse(1);
			return true;
		}
		else
		{
			return false;
		}*/
	}
	private void Start()
	{
		for (int i = 0; i < _playerLinks.Resources.Count; i++)
		{
			if (_playerLinks.Resources[i].Type == ResourceType.Energy)
			{
				_energy = (Energy)_playerLinks.Resources[i];
			}
			/*if (_playerLinks.Resources[i].Type == ResourceType.Rune)
			{
				_rune = (RuneComponent)_playerLinks.Resources[i];
			}*/
		}
	}

	private void Shoot()
	{
		Buff.AttackSpeed.ReductionPercentage(1 + _combo.GetMultipliedSpeed() / 100);

		Vector3 lookDir = _mousePos - _playerLinks.transform.position;
		float angle = Mathf.Atan2(lookDir.z, lookDir.x) * Mathf.Rad2Deg - 90f;
		if( _combo.MakeHit(null, AbilityForm.Magic, 1, 0, 0))
		{
			Debug.LogError("some talents i guess in ice cloud");
			//_playerLinks.RuneComponent.IceCloudBonus();
		}

		Buff.AttackSpeed.IncreasePercentage(1 + _combo.GetMultipliedSpeed() / 100);

		CmdCreateProjecttile(angle, _energy.CurrentValue);
		ClearData();
	}

	[Command]
	private void CmdCreateProjecttile(float angle, float manaValue)
	{
		IceCloudProjectile projectile = Instantiate(_projectile, gameObject.transform.position, Quaternion.Euler(0, -angle, 0));
		SceneManager.MoveGameObjectToScene(projectile.gameObject, _hero.NetworkSettings.MyRoom);
		projectile.Init(_playerLinks, manaValue, false, this);	

		NetworkServer.Spawn(projectile.gameObject);

		RpcInit(projectile.gameObject, manaValue);
	}

	[ClientRpc]
	private void RpcInit(GameObject obj, float manaValue)
	{
		obj.GetComponent<IceCloudProjectile>().Init(_playerLinks, manaValue, false, this);
	}

	public void TalentBoostDmg(bool value)
	{
		_boostDmg = value;
	}

	protected override IEnumerator PrepareJob()
	{
		while (float.IsPositiveInfinity(_mousePos.x))
		{
			if (GetMouseButton)
			{
				//if(GetTarget()  == null) yield return null;
				if (GetTarget().isCharater)
				{
					if (GetTarget().character == null)
					{
						//_mousePos = GetTarget().Position;
					}
					else
					{
						_mousePos = GetTarget().character.transform.position;
					}
				}
				else
				{
					_mousePos = GetTarget().Position;
				}
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
