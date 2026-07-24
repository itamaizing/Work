using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathSpiral : Skill,IEnergyDamagable
{
    [SerializeField] private DeathSpiralProjectile _projectile;

    private float _damageToCharge = 30f;
    private float _baseHealCorpse = 30f;
    private float _maxEnergyCost = 30f;
    private float _energyToDamage = 1f;
    private float _energyToHeal = 0.5f;

    public bool IsStreamSkill { get; }
    public bool IsFrostEnergyApplied { get; }

    private const int MaxCharges = 3;
    private const float AnimDuration = 0.8f;
    private const float SearchRadius = 0.5f;

    private RuneComponent _rune;
    private Energy _energy;
    private int _currentCharges;

    private float _currentAccumulatedDamage;

    private bool IsAllyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");

    protected override bool IsCanCast => _currentCharges > 0 && _rune.CurrentValue >= 1f;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    
    #region SecondaryDeathSpiral

    private const float SecondaryProjectileDamageMultiplier = 0.75f;
    private bool _isSecondaryProjectile;

    public void EnableSecondaryProjectileTalent(bool value)
    {
	    if(_isSecondaryProjectile == value) return;
	    _isSecondaryProjectile = value;
	    CmdEnableSecondaryProjectile(_isSecondaryProjectile);
    }

    [Command]
    private void CmdEnableSecondaryProjectile(bool value)
    {
	    _isSecondaryProjectile = value;
    }

    #endregion

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);

        _rune = (RuneComponent)Hero.Resources[ResourceType.Rune];
        _energy = (Energy)Hero.Resources[ResourceType.Energy];

        _currentCharges = MaxCharges;
        _maxCharges = MaxCharges;
        CheckChargers();
        
	    _hero.DamageTracker.OnDamageTracked += TrackDamage;
    }

    private void OnDestroy()
    {
	    _hero.DamageTracker.OnDamageTracked -= TrackDamage;
    }

    #region Charges

    private void AddCharge()
    {
        if (_currentCharges < _maxCharges)
            _currentCharges++;

        CheckChargers();
    }

    private void CheckChargers()
    {
        Disactive = _currentCharges <= 0;
        Charges.SendCurrentChange(_currentCharges);
    }

    protected override void UseCooldownOrCharges()
    {
        if (_currentCharges <= 0) return;
        _currentCharges--;
        CheckChargers();
    }

    private void TrackDamage(Damage damage, GameObject target)
    {
	    //if (!isServer) return;
        if (damage.Value <= 0) return;
		if(damage.DamageKey == "DeathSpiral") return;
        _currentAccumulatedDamage += damage.Value;

        while (_currentAccumulatedDamage >= _damageToCharge)
        {
            _currentAccumulatedDamage -= _damageToCharge;
            RpcOnDamageAccumulate(target);
        }
    }

    [TargetRpc]
    private void RpcOnDamageAccumulate(GameObject target)
    {
	    AddCharge();
    }

    #endregion

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0)
            Targeting.SetTarget((Character)targetInfo.GetTargets()[0]);
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();

        while (Targeting.GetTempTarget()?.Character == null)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget(Targeting.GetMousePoint(), SearchRadius, false);

                if (Targeting.GetTempTarget()?.Character != null)
                {
                    var target = Targeting.GetTempTarget()?.Character;

                    if (IsAllyTarget(target) && target is not MinionComponent && target != Hero)
                    {
                        Targeting.ClearTempTarget();
                    }
                    else
                    {
                        Hero.Move.LookAtTransform(target.transform);
                        break;
                    }
                }
            }
            yield return null;
        }

		targetInfo.AddTarget(Targeting.GetTempTarget()?.Character);
        callbackDataSaved?.Invoke(targetInfo);
        Targeting.ClearTempTarget();
    }

    protected override IEnumerator CastJob()
    {
        UseCooldownOrCharges();
        ConsumeRune();

        Character target = Targeting.GetTarget()?.Character;
        if (target == null) yield break;

        Hero.Animator.SetTrigger("Throw");
        yield return new WaitForSeconds(AnimDuration);

        if (target.IsDead || (target is MinionComponent && target.Abilities.GetSkill<DeadStrike>() != null))
        {
            yield return StartCoroutine(CastOnCorpse(target));
        }
        else
        {
            yield return StartCoroutine(CastOnEnemy(target));
        }

        Targeting.ClearTarget();
    }

    private void ConsumeRune()
    {
        _rune.CmdUse(1f);
    }

    private IEnumerator CastOnEnemy(Character enemy)
    {
        float energyToUse = Mathf.Min(_energy.CurrentValue, _maxEnergyCost);
        float additionalDamage = energyToUse * _energyToDamage;
        float totalDamage = Damage + additionalDamage;

        CmdShootProjectile(enemy, totalDamage, isCorpse: false,_isSecondaryProjectile,transform.position);

        _energy.CmdUse(energyToUse);

        yield return null;
    }

    private IEnumerator CastOnCorpse(Character corpse)
    {
        float energyToUse = Mathf.Min(_energy.CurrentValue, _maxEnergyCost);
        float additionalHeal = energyToUse * 2f;
        float totalHeal = _baseHealCorpse + additionalHeal;

        CmdShootProjectile(corpse, totalHeal, isCorpse: true,_isSecondaryProjectile,transform.position);

        _energy.CmdUse(energyToUse);

        yield return null;
    }

    [Command]
    private void CmdShootProjectile(Character target, float value, bool isCorpse, bool isSecondary, Vector3 spawnPos)
    {
        if (_projectile == null) return;

        Vector3 dir = (target.transform.position - transform.position).normalized;
        float angle = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg - 90f;

        DeathSpiralProjectile proj = Instantiate(_projectile, spawnPos, Quaternion.Euler(0, -angle, 0));
        NetworkServer.Spawn(proj.gameObject);
        proj.Init(Hero, 0, false, this);
        proj.SetTarget(target);
        proj.SetDamageOrHeal(value, isCorpse);
        proj.SetAsSecondary(false,isSecondary);
        RpcInit(proj.gameObject,target.gameObject,value,isCorpse,isSecondary);
    }

    [ClientRpc]
    private void RpcInit(GameObject obj, GameObject targetObj, float value, bool isHeal, bool isSecondary)
    {
	    if (obj.TryGetComponent<DeathSpiralProjectile>(out DeathSpiralProjectile spiral))
	    {
		    var target = targetObj.GetComponent<Character>();
		    spiral.Init(Hero, 0, false, this);
		    spiral.SetDamageOrHeal(value, isHeal);
		    spiral.SetTarget(target);
		    spiral.SetAsSecondary(false,isSecondary);
	    }
    }
    
    public void SpawnSecondaryParticles(float damage,Vector3 position)
    {
	    RpcCheckForSecondary(damage * SecondaryProjectileDamageMultiplier,position);
    }
    
    [ClientRpc]
    private void RpcCheckForSecondary(float newDamage,Vector3 positionToCheck)
    {
	    Collider[] hits = Physics.OverlapSphere(positionToCheck, 3, _targetsLayers);
	    foreach (var hit in hits)
	    {
		    if (hit.TryGetComponent<Character>(out Character enemy) && enemy != _hero && enemy.CharacterState.CheckForState(States.PortalDarkness))
		    {
			    CmdShootProjectile(enemy, newDamage, isCorpse: false, isSecondary: false, positionToCheck);
		    }
	    }
    }

    #region OLD

	//private GameObject _target;
	/*private bool _superCharge = false;
	private bool _inTheRow = false;
	private bool _talentSecondAttack = false;
	private bool _talentBoostHPBOdy = false;
	private bool _talentHitState = false;
	private bool _talentPlague = false;
	private bool _talentChragesPlague = false;
	private bool _talentCorpseDeath = false;
	private bool _talentCorpseBoostExplode;
	private bool _firstShot = true;*/
	/*private void Timer()
{
	if (!_inTheRow) return;

	_timer-= Time.deltaTime;
	if(_timer <= 0)
	{
		_firstShot = true;
		_inTheRow = false;
		_timer = 1; 
	}
}*/
	/*
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
	*/
	
	/*private void BasicShoot()
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
}*/
	
	/*private void PlagueAbsorptionCharge()
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
}*/
	
	/*[Command]
public void CmdUseCharge(int value)
{
	if (Chargers - value >= 0)
	{
		Chargers = Chargers - 1;
	}
}*/
	
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

	/*[Command]
	private void Shoot(float angle, bool inTheRow, Character target, bool talentBoostHpBody, bool talentHitState, bool talentPlague, bool talentChargesPlague, bool superCharge, bool corpseDeath, bool corpseBoostExplode)
	{
		//Debug.Log(target + " target name ");
		DeathSpiralProjectile projectile = Instantiate(_projectile, gameObject.transform.position, Quaternion.Euler(0, -angle, 0));
		projectile.Init(_playerLinks, 0, false, this);
		projectile.SetTarget(target);
		projectile.Talents(talentBoostHpBody, talentHitState, inTheRow, talentPlague, talentChargesPlague, superCharge,_isSecondaryProjectile);
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
		projectile.Talents(talentBoostHpBody, talentHitState, inTheRow, talentPlague, talentChargesPlague, superCharge,_isSecondaryProjectile);
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
		return target;
	}*/
	#endregion
}
