using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class IceSword : CloseCombatSkill
{
	[SerializeField] private float _damage = 15f;
	//[SerializeField] private GameObject _basePlayer;
	[SerializeField] private Character _playerLinks;
	[SerializeField] private DeathSpiral _deathSpiral;
	[SerializeField] private SeriesOfStrikes _seriesOfStrikes;
	[SerializeField] private GameObject _sword;
	[SerializeField] private AudioClip audioClip;


	private int _hitInTheRow = 0;
	private float _additionalDamage = 0;
    [SerializeField] private float _maxAdditionalCost = 10f;
	private Character _oldtarget;
	//private Character _target;
	private float _duration = 3;
	private Energy _energy;
	private Coroutine coroutineSwordTime;
	private RuneComponent _rune;
	private bool _critDmg = false;
	private AudioSource _audioSource;
	protected override bool IsCanCast => IsCanCastCheck();

	protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("IceSword");

	private bool IsCanCastCheck()
	{
		if (Targeting.GetTarget()?.Character == null) return false;

		if (Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position) > AreaInfo.Radius)
		{
			return false;
		}
		return true;
	}

	private void Start()
	{
        _audioSource = GetComponent<AudioSource>();

        //_energy = (Energy)_playerLinks.Resources[ResourceType.Energy];
        //_rune = (RuneComponent)_playerLinks.Resources[ResourceType.Rune];
	}

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        Targeting.SetTarget((Character)targetInfo.GetTargets()[0]);
    }

	protected override IEnumerator CastJob()
	{
		if (_energy == null)
			_energy = (Energy)Hero.Resources[ResourceType.Energy];

		if (_rune == null)
			_rune = (RuneComponent)Hero.Resources[ResourceType.Rune];

        _seriesOfStrikes.MakeHit(Targeting.GetTarget()?.Character, Info.AbilityForm, 0, 10, 0);
		if (Targeting.GetTarget()?.Character == _oldtarget)
		{
			_hitInTheRow++;
		}
		else
		{
			_hitInTheRow = 1;
			_oldtarget = Targeting.GetTarget()?.Character;
		}
		if (_hitInTheRow > 2)
		{
			_deathSpiral.AddCharge();
			_hitInTheRow = 0;
		}
		ApplyDamage();
		CmdAdd(Targeting.GetTarget()?.Character.gameObject);
		yield return null;
	}

	protected override void ClearData()
	{
		Targeting.ClearTarget();
		Targeting.ClearTempTarget();
		//_target = null;
	}

	private void ApplyDamage()
	{
		//Debug.Log("111111111111");
		/*float energyBonus = 0;

        if (_energy.CurrentValue >= 10)
			energyBonus = Mathf.Min(_energy.CurrentValue, 10);*/
		
		//_energy.CmdUse(energyBonus);

		//float totalDamage = _damage + energyBonus;
		float totalDamage = _damage + _additionalDamage;

		Damage damage2 = new Damage
		{
			Value = totalDamage,
			Type = DamageType.Physical,
			PhysicAttackType = AttackRangeType.RangeAttack,
		};
		Debug.Log("Damage " + totalDamage);

		if (_critDmg && Targeting.GetTarget().Character.CharacterState.CheckForState(States.Frozen))
		{
			damage2.Value *= (Random.Range(0, 100) < 15) ? 1.8f : 1.1f;
		}

		CmdApplyDamage(damage2, Targeting.GetTarget()?.Character.gameObject);

		_energy.SumDamageMake(damage2.Value);
		_rune.SumDamageMake(damage2.Value);
	}

	private IEnumerator ISwordTimer()
    {
		Coroutine currentCoroutine = coroutineSwordTime;
		yield return new WaitForSeconds(2.5f);

		if (currentCoroutine == coroutineSwordTime)
		{
			_sword.SetActive(false);
			coroutineSwordTime = null;
		}
	}

	[Command]
	private void CmdAdd(GameObject enemy)
	{
		Character enemyCharacter = enemy.GetComponent<Character>();
		RpcPlayShotSound();
		enemyCharacter.CharacterState.AddState(States.Frozen, _duration, 0, _playerLinks.gameObject, name);
	}

	[ClientRpc]
	private void RpcPlayShotSound()
	{
		if (_audioSource != null && audioClip != null) _audioSource.PlayOneShot(audioClip);
	}

	public void TalentCritDmg(bool value)
	{
		_critDmg = value;
	}

	public void CorutineSwordTimeStart()
	{
        if (coroutineSwordTime != null) StopCoroutine(coroutineSwordTime);
        coroutineSwordTime = StartCoroutine(ISwordTimer());
    }

	public void IceSwordCast()
	{
		AnimStartCastCoroutine();
	}

	public void IceSwordEnd()
	{
		AnimCastEnded();
	}
    protected override bool TryPayCost(List<SkillResourceCost> skillEnergyCosts, bool startCooldown = true)
    {
		if (!IsHaveResourceOnSkill)
		{
			return false;
		}

        _additionalDamage = 0;

        foreach (var skillCost in skillEnergyCosts)
        {
			var baseCost = skillCost.value;

            if (_energy.CurrentValue > baseCost)
			{
                _additionalDamage = Mathf.Min(_energy.CurrentValue-baseCost, _maxAdditionalCost);
                //Debug.Log($"Add damage {_additionalDamage}, | currEnergy {_energy.CurrentValue} ");
                _energy.CmdUse(_additionalDamage);
            }
            _energy.CmdUse(Buff.ManaCost.GetBuffedValue(baseCost));
        }

		if (startCooldown)
		{
			IncreaseSetCooldown(CooldownTime);
			Cooldown.SetIncreased(Cooldown.CooldownTime, shouldModify: false);
		}

        if (!_useChargesAsComboPart) TryUseCharge();
        return true;
    }
}
