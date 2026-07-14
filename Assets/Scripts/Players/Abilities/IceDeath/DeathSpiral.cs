using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathSpiral : Skill
{
	[SerializeField] private DeathSpiralProjectile _projectile;
	[SerializeField] private HeroComponent _playerLinks;
	[SerializeField] private SeriesOfStrikes _seriesOfStrikes;
	[SerializeField] private SpawnComponent _spawnComponent;
	[SerializeField] private PlagueAbsorption _plagueAbsorption;
	[SerializeField] private DamageTracker _damageTracker;
	[SerializeField] private float _damageToCharge = 30f;

	private Heal _heal;
	private float _timer = 1f;
	private Vector3 _mousePos = Vector3.positiveInfinity;
	//private GameObject _target;
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

	[SyncVar(hook = nameof(OnChargesChanged))]
	private int _chargesSpiral;

	private float _currentAccumulatedDamage;

	private const int MaxCharges = 3;

	protected override bool IsCanCast
	{
		get
		{
			if (Targeting.GetTarget()?.Character == null) return false;
			if (_chargesSpiral <= 0) return false;
			return Vector3.Distance(Hero.transform.position, Targeting.GetTarget().Character.transform.position) <= AreaInfo.CastLength;
		}
	}

	protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;

	private void Start()
	{
		if (_damageTracker != null) _damageTracker.OnDamageTracked += TrackDamage;
	}

	private void Update()
	{
		Timer();
	}

	protected override void Awake()
	{
		base.Awake();

		_chargesSpiral = 0;
		_maxCharges = MaxCharges;
	}

	private void OnDestroy()
	{
		if (_damageTracker != null) _damageTracker.OnDamageTracked -= TrackDamage;
	}

	private void OnChargesChanged(int oldValue, int newValue)
	{
		Charges.SendCurrentChange(newValue);
	}

	public override void LoadTargetData(TargetInfo targetInfo)
	{
		if (targetInfo.GetTargets().Count > 0) Targeting.SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
	}

	protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
	{
		while (Targeting.GetTempTarget()?.Character == null)
		{
			if (GetMouseButton)
			{
				Targeting.FindTempTarget();
			}

			yield return null;
		}

		Targeting.SetTarget(Targeting.GetTempTarget()?.Character);
		TargetInfo targetInfo = new TargetInfo();
		targetInfo.AddTarget(Targeting.GetTarget()?.Character);
		callbackDataSaved(targetInfo);
	}

	protected override IEnumerator CastJob()
	{
		UseSpiralCharge(1);

		if (_plagueAbsorption.Charges >= 1)
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
		Targeting.ClearTarget();
		Targeting.ClearTempTarget();

		_mousePos = Vector3.positiveInfinity;
	}

	private void UseSpiralCharge(int value = 1)
	{
		_chargesSpiral -= value;
		_chargesSpiral = Mathf.Max(0, _chargesSpiral);

		Charges.SendCurrentChange(_chargesSpiral);
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
			_seriesOfStrikes.MakeHit(null, Info.AbilityForm.Magic, 1, 0);
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
			_seriesOfStrikes.MakeHit(null, Info.AbilityForm.Magic, 1, 0);
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
			_seriesOfStrikes.MakeHit(null, Info.AbilityForm.Magic, 1, 0);
			Shoot(angle, _inTheRow);
		}
		
	}*/

	[Command]
	private void Shoot(float angle, bool inTheRow, Character target, bool talentBoostHpBody, bool talentHitState, bool talentPlague, bool talentChargesPlague, bool superCharge, bool corpseDeath, bool corpseBoostExplode)
	{
		//Debug.Log(target + " target name ");
		DeathSpiralProjectile projectile = Instantiate(_projectile, gameObject.transform.position, Quaternion.Euler(0, -angle, 0));
		projectile.Init(_playerLinks, 0, false, this);
		projectile.SetTarget(target);
		projectile.Talents(talentBoostHpBody, talentHitState, inTheRow, talentPlague, talentChargesPlague, superCharge);
		projectile.Talents(corpseDeath, corpseBoostExplode);

		NetworkServer.Spawn(projectile.gameObject);

		RpcInit(projectile.gameObject, target, talentBoostHpBody, talentHitState, inTheRow, talentPlague, talentChargesPlague, superCharge, corpseDeath, corpseBoostExplode);
		_superCharge = false;
	}

	[ClientRpc]
	private void RpcInit(GameObject obj, Character target, bool talentBoostHpBody, bool talentHitState, bool inTheRow, bool talentPlague, bool talentChargesPlague, bool superCharge, bool corpseDeath, bool corpseBoostExplode)
	{
		//Debug.Log(target + " target name ");
		DeathSpiralProjectile projectile = obj.GetComponent<DeathSpiralProjectile>();
		projectile.Init(_playerLinks, 0, false, this);
		projectile.SetTarget(target);
		projectile.Talents(talentBoostHpBody, talentHitState, inTheRow, talentPlague, talentChargesPlague, superCharge);
		projectile.Talents(corpseDeath, corpseBoostExplode);
		_superCharge = false;
	}

	private GameObject GetRaycastTargetShadow(bool isCanTargetHimself = false)
	{
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit[] rayHit = Physics.RaycastAll(ray, 100f, Targeting.Layer);

		foreach (var hit in rayHit)
		{
			Debug.Log(hit.collider.gameObject.name);
		}
		GameObject target = null;

		foreach (var item in rayHit)
		{
			if (rayHit.Length > 0 && item.transform.TryGetComponent<Character>(out Character enemy))
			{
				target = enemy.gameObject;

				if (isCanTargetHimself == false && target.transform == _hero.Health.transform)
				{
					target = null;
				}
			}

			if(rayHit.Length > 0 && item.transform.TryGetComponent<IceShadowObject>(out IceShadowObject shadow))
			{
				target = shadow.gameObject;
			}
		}
		//_tempTargetbase = target;
		return target;
	}

	private void PlagueAbsorptionCharge()
	{
		//Debug.Log("PLAGUE Absorption ATTACK");
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
		_seriesOfStrikes.MakeHit(null, Info.AbilityForm, 1, 0, 0);


		Shoot(angle, _inTheRow, Targeting.GetTarget()?.Character, _talentBoostHPBOdy, _talentHitState, _talentPlague, _talentChragesPlague, _superCharge, _talentCorpseDeath, _talentCorpseBoostExplode);
	}

	private void BasicShoot()
	{
		//Debug.Log("FIRST ATTACK");
		_firstShot = false;
		_superCharge = false;
		_inTheRow = true;
		Vector3 lookDir = _mousePos - _playerLinks.transform.position;
		float angle = Mathf.Atan2(lookDir.z, lookDir.x) * Mathf.Rad2Deg - 90f;
		_seriesOfStrikes.MakeHit(null, Info.AbilityForm, 1, 0, 0);
		Shoot(angle, _inTheRow, Targeting.GetTarget()?.Character, _talentBoostHPBOdy, _talentHitState, _talentPlague, _talentChragesPlague, _superCharge, _talentCorpseDeath, _talentCorpseBoostExplode);
	}

	private void SecondAttact()
	{
		//Debug.Log("SECOND ATTACK");
		_superCharge = false;
		Vector3 lookDir = _mousePos - _playerLinks.transform.position;
		float angle = Mathf.Atan2(lookDir.z, lookDir.x) * Mathf.Rad2Deg - 90f;
		_seriesOfStrikes.MakeHit(null, Info.AbilityForm, 1, 0, 0);
		Shoot(angle, _inTheRow, Targeting.GetTarget()?.Character, _talentBoostHPBOdy, _talentHitState, _talentPlague, _talentChragesPlague, _superCharge, _talentCorpseDeath, _talentCorpseBoostExplode);
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

	private void TrackDamage(Damage damage, GameObject target)
	{
		if (!isServer) return;

		if (target == null) return;
		if (damage.Type != DamageType.Physical) return;

		_currentAccumulatedDamage += damage.Value;

		while (_currentAccumulatedDamage >= _damageToCharge)
		{
			if (_chargesSpiral >= MaxCharges)
			{
				_currentAccumulatedDamage = 0;
				return;
			}

			_currentAccumulatedDamage -= _damageToCharge;

			_chargesSpiral++;

			Charges.SendCurrentChange(_chargesSpiral);

			Debug.Log($"DeathSpiral charge added {_chargesSpiral}/{MaxCharges}");
		}
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
		//if()
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
