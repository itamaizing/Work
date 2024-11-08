using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathSpiral : Skill
{
	[SerializeField] private DeathSpiralProjectile _projectile;
	[SerializeField] private HeroComponent _playerLinks;
	[SerializeField] private SeriesOfStrikes _seriesOfStrikes;
	[SerializeField] private SpawnComponent _spawnComponent;
	[SerializeField] private PlagueAbsorption _plagueAbsorption;

	private Heal _heal;
	private float _timer = 1f;
	private Vector3 _mousePos = Vector3.positiveInfinity;
	private GameObject _target;
	private bool _superCharge = false;
	private bool _inTheRow = false;
	private bool _talentSecondAttack = false;
	private bool _talentBoostHPBOdy = false;
	private bool _talentHitState = false;
	private bool _talentPlague = false;
	private bool _talentChragesPlague = false;
	private bool _talentCorpseDeath = false;
	private bool _talentCorpseBoostExplode;
	private bool _firstShot = true;

	protected override bool IsCanCast => Chargers > 0;

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;

	private void Update()
	{
		Timer();
	}
	protected override IEnumerator PrepareJob()
	{
		while (_target == null)
		{
			if (GetTarget().character != null)
			{
				_target = GetTarget().character.gameObject;
				Debug.Log(_target + " target name ");
			}
			yield return null;
		}
	}

	protected override IEnumerator CastJob()
	{
		if(_plagueAbsorption.Charges>= 1)
		{
			_plagueAbsorption.CmdUseCharge(1);
			PlagueAbsorptionCharge();
		}
		else if (_inTheRow && _talentSecondAttack)
		{
			SecondAttact();
		}
		else
		{
			BasicShoot();
		}
		yield return null;
	}
	protected override void ClearData()
	{
		_target = null;
		_mousePos = Vector3.positiveInfinity;
	}

	/*protected override void Cast()
	{
		if(_plagueAbsorption.UseCharge(1))
		{
			_superCharge = true;
			_inTheRow = true;

			RaycastHit2D[] rayHit = Physics2D.RaycastAll(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero, 99, _targetsLayers);

			foreach (var item in rayHit)
			{
				if (rayHit.Length > 0 && item.transform.TryGetComponent<Character>(out Character enemy))
				{
					if (enemy == _playerLinks)
					{
						if (_inTheRow)
						{
							_playerLinks.Health.Heal(10);
							return;
						}
						else
						{
							_playerLinks.Health.Heal(20);
							return;
						}
					}
				}
			}

			_mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			Vector2 lookDir = _mousePos - _playerLinks.Rigidbody2D.position;
			float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
			_seriesOfStrikes.MakeHit(null, AbilityForm.Magic, 1, 0);
			Debug.Log("SUPER CHARGE TEST");
			//Shoot(angle, _inTheRow);
		}

		else if (_inTheRow && _talentSecondAttack)
		{
			_superCharge = false;
			_mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			Debug.LogError("fix");
			Vector2 lookDir = _mousePos - _playerLinks.Rigidbody2D.position;
			float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
			_seriesOfStrikes.MakeHit(null, AbilityForm.Magic, 1, 0);
			Shoot(angle, _inTheRow);
		}
		//else if (_playerLinks.RuneComponent.RemoveRune(2, this))
		{
			_superCharge = false;
			_currentChargers--;
			_inTheRow = true;
			_mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			Vector2 lookDir = _mousePos - _playerLinks.Rigidbody2D.position;
			float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
			_seriesOfStrikes.MakeHit(null, AbilityForm.Magic, 1, 0);
			Shoot(angle, _inTheRow);
		}
		
	}*/

	[Command]
	private void Shoot(float angle, bool inTheRow, GameObject target)
	{
		Debug.Log(target + " target name ");
		DeathSpiralProjectile projectile = Instantiate(_projectile, gameObject.transform.position, Quaternion.Euler(0, -angle, 0));
		SceneManager.MoveGameObjectToScene(projectile.gameObject, _hero.NetworkSettings.MyRoom);
		projectile.Init(_playerLinks, 0, false, this);
		projectile.SetTarget(target);
		projectile.Talents(_talentBoostHPBOdy, _talentHitState, inTheRow, _talentPlague, _talentChragesPlague, _superCharge);
		projectile.Talents(_talentCorpseDeath, _talentCorpseBoostExplode);

		NetworkServer.Spawn(projectile.gameObject);

		RpcInit(projectile.gameObject, target);
		_superCharge = false;
	}

	[ClientRpc]
	private void RpcInit(GameObject obj, GameObject target)
	{
		Debug.Log(target + " target name ");
		DeathSpiralProjectile projectile = obj.GetComponent<DeathSpiralProjectile>();
		projectile.Init(_playerLinks, 0, false, this);
		projectile.SetTarget(target);
		projectile.Talents(_talentBoostHPBOdy, _talentHitState, _inTheRow, _talentPlague, _talentChragesPlague, _superCharge);
		projectile.Talents(_talentCorpseDeath, _talentCorpseBoostExplode);
		_superCharge = false;
	}

	private void PlagueAbsorptionCharge()
	{
		_superCharge = true;
		_inTheRow = true;

		RaycastHit[] rayHit = Physics.RaycastAll(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero, 99, _targetsLayers);

		foreach (var item in rayHit)
		{
			if (rayHit.Length > 0 && item.transform.TryGetComponent<Character>(out Character enemy))
			{
				if (enemy == _playerLinks)
				{
					if (_inTheRow)
					{
						var heal = new Heal { Value = 10 };
						_playerLinks.Health.Heal(ref heal,name);
						return;
					}
					else
					{
						var heal = new Heal { Value = 20 };
						_playerLinks.Health.Heal(ref heal,name);
						return;
					}
				}
			}
		}
		Vector3 lookDir = _mousePos - _playerLinks.transform.position;
		float angle = Mathf.Atan2(lookDir.z, lookDir.x) * Mathf.Rad2Deg - 90f;
		_seriesOfStrikes.MakeHit(null, AbilityForm.Magic, 1, 0);
		Shoot(angle, _inTheRow, _target);
	}

	private void BasicShoot()
	{
		_firstShot = false;
		_superCharge = false;
		_inTheRow = true;
		Vector3 lookDir = _mousePos - _playerLinks.transform.position;
		float angle = Mathf.Atan2(lookDir.z, lookDir.x) * Mathf.Rad2Deg - 90f;
		_seriesOfStrikes.MakeHit(null, AbilityForm.Magic, 1, 0);
		Shoot(angle, _inTheRow, _target);
	}

	private void SecondAttact()
	{
		_superCharge = false;
		Vector3 lookDir = _mousePos - _playerLinks.transform.position;
		float angle = Mathf.Atan2(lookDir.z, lookDir.x) * Mathf.Rad2Deg - 90f;
		_seriesOfStrikes.MakeHit(null, AbilityForm.Magic, 1, 0);
		Shoot(angle, _inTheRow, _target);
	}

	public void AddCharge()
	{
		if (Chargers < _maxCharges)
		{
			Chargers = Chargers + 1;
		}
	}

	private void Timer()
	{
		if (!_inTheRow) return;

		_timer-= Time.deltaTime;
		if(_timer <= 0)
		{
			_firstShot = true;
			_inTheRow = false;
			_timer = 1; 
		}
	}

	protected override bool TryPayCost(List<SkillEnergyCost> skillEnergyCosts, bool startCooldown = true)
	{
		if (_firstShot && TryUseCharge())
		{
			foreach (var skillCost in _skillEnergyCosts)
			{
				var resource = _hero.Resources.First(r => r.Type == skillCost.resourceType);
				resource.CmdUse(Buff.ManaCost.GetBuffedValue(skillCost.resourceCost));
			}
			_firstShot = false;
		}
		return true;
	}

	[Command]
	public void CmdUseCharge(int value)
	{
		if (Chargers - value >= 0)
		{
			Chargers = Chargers - 1;
		}
	}

	protected override IEnumerator RechargeCoroutine()
	{
		_rechargeJob = null;
		yield return null;
	}

	public void TalentMaxCharges(int maxChargesValue)
	{
		_maxCharges = maxChargesValue;
	}

	public void TalentSecondAttack(bool value)
	{
		_talentSecondAttack = value;
	}

	public void TalentBoostHpCorpse(bool value)
	{
		_talentBoostHPBOdy = value;
	}

	public void TalentHitState(bool value)
	{
		_talentHitState = value;
	}

	public void TalentPlague(bool value)
	{
		_talentPlague = value;
	}

	public void TalentChargesPlague(bool value)
	{
		_talentChragesPlague = value;
	}

	public void TalentCosrpseDeath(bool value)
	{
		_talentCorpseDeath = value;
	}

	public void TalentCorpseBoostExplode(bool value)
	{
		_talentCorpseBoostExplode = value;
	}

	public void TalentSuperCharge(bool value)
	{
		_superCharge = value;
	}
}
