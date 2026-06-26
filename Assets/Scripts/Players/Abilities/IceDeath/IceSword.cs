using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class IceSword : CloseCombatSkill,IEnergyDamagable, IComboSeriesParticipatingSkill
{
	[SerializeField] private float _damage = 15f;
	//[SerializeField] private GameObject _basePlayer;
	[SerializeField] private Character _playerLinks;
	[SerializeField] private DeathSpiral _deathSpiral;
	[SerializeField] private GameObject _sword;
	[SerializeField] private AudioClip audioClip;

	[SerializeField] private float _baseEnergyCost = 40f;
	[SerializeField] private float _maxAdditionalCost = 10f;

	private int _hitInTheRow = 0;
	private float _additionalDamage = 0;
	private Character _oldtarget;
	//private Character _target;
	private float _duration = 12;
	private Energy _energy;
	private Coroutine coroutineSwordTime;
	private RuneComponent _rune;
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
		Character targetCharacter = Targeting.GetTarget()?.Character;
		if (targetCharacter == null) yield break;

		if (!ConsumeEnergy())
		{
			TryCancel(true);
			yield break;
		}
		
		OnSeriesDamaged?.Invoke(targetCharacter.gameObject,this);

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
			_hitInTheRow = 0;
		}
		ApplyDamage(targetCharacter);
		CmdAdd(Targeting.GetTarget()?.Character.gameObject,_seriesComplete);
		yield return null;
		_seriesComplete = false;
	}

	protected override void ClearData()
	{
		Targeting.ClearTarget();
		Targeting.ClearTempTarget();
		//_target = null;
	}

	private void ApplyDamage(Character targetCharacter)
	{
		float totalDamage = _damage + _additionalDamage;

		Damage damage2 = new Damage
		{
			Value = totalDamage,
			Type = DamageType.Physical,
			PhysicAttackType = AttackRangeType.RangeAttack,
		};
		
		CmdApplyDamage(damage2, targetCharacter.gameObject);

		//_energy.SumDamageMake(damage2.Value);
		//_rune.SumDamageMake(damage2.Value);
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
	private void CmdAdd(GameObject enemy, bool isFinal)
	{
		Character enemyCharacter = enemy.GetComponent<Character>();
		RpcPlayShotSound();
		if (!isFinal)
			enemyCharacter.CharacterState.AddState(States.Cooling, _duration, 0, Schools.Water, _playerLinks.gameObject, name);
		else
		{
			for (int i = 0; i < _frostingStacks; i++)
			{
				enemyCharacter.CharacterState.AddState(States.Cooling, _duration, 0, Schools.Water, _playerLinks.gameObject, name);
			}
		}
	}

	[ClientRpc]
	private void RpcPlayShotSound()
	{
		if (_audioSource != null && audioClip != null) _audioSource.PlayOneShot(audioClip);
	}

	public void IceSwordCast()
	{
		AnimStartCastCoroutine();
	}

	public void IceSwordEnd()
	{
		AnimCastEnded();
	}

	private void EnsureResources()
	{
		if (_energy == null) _energy = (Energy)Hero.Resources[ResourceType.Energy];
		if (_rune == null) _rune = (RuneComponent)Hero.Resources[ResourceType.Rune];
	}

	private bool ConsumeEnergy()
	{
		EnsureResources();

		float baseCost = Buff.ManaCost.GetBuffedValue(_baseEnergyCost);
		float currentEnergy = _energy.CurrentValue;

		if (currentEnergy < baseCost)
			return false;

		float additionalEnergy = Mathf.Clamp(currentEnergy - baseCost, 0f, _maxAdditionalCost);

		_additionalDamage = additionalEnergy;

		float totalEnergyToUse = baseCost + additionalEnergy;

		if (Cost.TryPaySingle(totalEnergyToUse, ResourceType.Energy, shouldModify: false))
		{
			_totalEnergySpend = totalEnergyToUse;
			return true;
		}

		return false;
	}

	protected override bool CheckResourcesOnSkill()
	{
		EnsureResources();
		float baseCost = Buff.ManaCost.GetBuffedValue(_baseEnergyCost);
		return _energy.CurrentValue >= baseCost;
	}

	#region Series

	private float _frostingStacks = 5f;
	private float _totalEnergySpend = 0f;
	private bool _seriesComplete = false;
	public event IComboSeriesParticipatingSkill.OnBeforeApplyDamageDelegate OnBeforeApplySeriesDamage;
	public event Action<GameObject, Skill> OnSeriesDamaged;
	public float EnergyCostOnHit => _totalEnergySpend;
	public float RuneCostOnHit { get; }
	public bool IsTicking { get; }

	public void OnSeriesHit(int hitCountInCurrentSeries, Character target)
	{
	}

	public void OnSeriesCompleted(Character target, int totalHits, float totalEnergySpent)
	{
		Debug.LogError("Series complete");
		_seriesComplete = true;
	}

	public void OnSeriesBroken(Character target)
	{
	}

	public void OnSeriesPotentialFinal(Skill skill, bool isPotentialFinal)
	{
	}

	#endregion
	public bool IsStreamSkill { get; }
	public bool IsFrostEnergyApplied { get; }

}
