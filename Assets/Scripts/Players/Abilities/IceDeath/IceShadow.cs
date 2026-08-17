using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IceShadow : Skill,IEnergyDamagable
{
	[Header("Ability properties")]
	[SerializeField] private IceShadowObject _shadow;
	[SerializeField] private IcyStream _icyStream;
	[SerializeField] private BlockOfIce _blockOfIce;
	[SerializeField] private CircularFrosting _circularFrosting;
	[ReadOnly][SerializeField] private IcyStreamShadow _icyStreamShadow;
	[SerializeField] private HeroComponent _playerLinks;
	[SerializeField] private AudioClip audioClip;
	//[SerializeField] private bool isTest = true;

	private IcyStream.IcyStreamState? _capturedState;
	private BlockOfIce.BlockOfIceState? _capturedBlockOfIceState; 

	private AudioSource _audioSource;
	private Energy _energy;
	//private RuneComponent _rune;
	private bool _lastHit = false;
	private bool _talentEvade = false;
	private bool _talentDamage = false;
	private bool _iceDeathInShadowTalent = false;
	private bool _evaded = false;
	private float _evadedTimer = 2f;
	private float _manaUsed = 0;
	private float _remainingDelayCircularFrostin;
	private float _blockOfIceRemainingCastTime = 0f;

	#region Const
	private const float MaxManaPerCast = 30f;
	private const float SpeedScaleDivisor = 100f;
	#endregion

	protected override bool IsCanCast => IsCanCastCheck();

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;

    private bool IsCanCastCheck()
	{
		return true;
	}

	private void Start()
	{
		_audioSource = GetComponent<AudioSource>();
	}

	private void OnEnable()
	{
		_playerLinks.Health.Evaded += Evaded;
	}

	private void OnDestroy()
	{
		_playerLinks.Health.Evaded -= Evaded;
	}
    public override void LoadTargetData(TargetInfo targetInfo)
    {
		if (targetInfo == null) return;
		if (targetInfo.GetTargets().Contains(Hero)) return;
		targetInfo.AddTarget(Hero);
	}

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
	    _capturedState = null;

	    if (_energy == null) _energy = (Energy)Hero.Resources[ResourceType.Energy];

	    if (_icyStream != null)
	    {
		    if (_icyStream.TryGetState(out var state)) _capturedState = state;

		    _icyStream.StopStream();
		    _icyStream.TryCancel(true);
	    }
	    
	    if (_blockOfIce != null && _blockOfIce.TryGetState(out var blockState))
	    {
		    _capturedBlockOfIceState = blockState;
		    _blockOfIce.TryCancel(true);
	    }

	    if (_circularFrosting != null) _circularFrosting.TryCancel(true);

	    TargetInfo targetInfo = new TargetInfo();
	    targetInfo.AddTarget(Hero);
	    callbackDataSaved(targetInfo);
	    yield return null;
    }

	protected override IEnumerator CastJob()
	{
		Shoot();
		yield return null;
	}

	protected override void ClearData()
	{
		_capturedState = null;
		_capturedBlockOfIceState = null;
	}

	private void Shoot()
	{
		bool triggeredFromStream = _capturedState.HasValue;
		bool triggeredFromBlockOfIce = _capturedBlockOfIceState.HasValue;
		float blockRemainingDelay = triggeredFromBlockOfIce ? _blockOfIce.RemainingCastDelay : 0f;
		bool triggeredFromFrosting = _circularFrosting != null && _circularFrosting.WasInterruptedInDelay;

		bool triggeredFromOtherSkill = triggeredFromStream || triggeredFromFrosting || triggeredFromBlockOfIce;

		if (triggeredFromFrosting)
		{
			_remainingDelayCircularFrostin = _circularFrosting.RemainingDelay;
		}

		if (!triggeredFromOtherSkill)
		{
			_manaUsed = Mathf.Min(_energy.CurrentValue, MaxManaPerCast);
			_energy.CmdUse(_manaUsed);
		}

		float bonusDuration = 0f;

		if (triggeredFromFrosting)
		{
			bonusDuration += _circularFrosting.RemainingDelay;
		}

		if (_capturedState.HasValue)
		{
			float tickTime = 0.3f;
			int remainingTicks = _capturedState.Value.MaxTicks - _capturedState.Value.CurrentTick;
			bonusDuration += remainingTicks * tickTime;
		}

		Character character = Targeting?.Target?.Character;

		Vector3 targetPos = _capturedBlockOfIceState.HasValue 
			? _capturedBlockOfIceState.Value.TargetPosition 
			: (character != null ? character.transform.position : Vector3.zero);

		CmdCreateProjecttile(
			_remainingDelayCircularFrostin, 
			0, 
			_manaUsed, 
			bonusDuration, 
			_lastHit, 
			_talentDamage,	
			_iceDeathInShadowTalent, 
			triggeredFromFrosting,
			triggeredFromStream,
			triggeredFromBlockOfIce,
			_capturedBlockOfIceState?.BonusDamage ?? 0f,
			targetPos,
			_capturedState?.CurrentTick ?? -1, 
			_capturedState?.MaxTicks ?? -1,
			character,
			blockRemainingDelay);
		
		_capturedBlockOfIceState = null;
	}

	[Command]
	private void CmdCreateProjecttile(
		float remainingDelay, 
		float angle, 
		float manaValue, 
		float streamBonus, 
		bool lastHit, 
		bool damage, 
		bool inShadow, 
		bool shouldSpawnCircularShadow, 
		bool shouldSpawnStreamShadow,
		bool shouldSpawnBlockOfIceShadow,
		float blockOfIceBonusDamage,
		Vector3 targetPos,
		int startTick, 
		int maxTicks, 
		Character targetIdentity,
		float blockRemainingDelay)
	{
		AnimatorStateInfo stateInfo = _playerLinks.Animator.GetCurrentAnimatorStateInfo(0);
		int animationHash = stateInfo.fullPathHash;
		float normalizedTime = stateInfo.normalizedTime % 1f;
		float velocityX = _playerLinks.Animator.GetFloat(HashAnimPlayer.VelocityX);
		float velocityZ = _playerLinks.Animator.GetFloat(HashAnimPlayer.VelocityZ);
		Quaternion rotation = _playerLinks.transform.rotation;

		Vector3 basePosition = _playerLinks.transform.position;

		if (shouldSpawnCircularShadow) _circularFrosting.PayEnergyOnInterruptedDelay();

		Character target = null;
		if (targetIdentity != null) target = targetIdentity.GetComponent<Character>();

		if (lastHit)
		{
			Vector3 right = _playerLinks.transform.right;
			Vector3 left = -_playerLinks.transform.right;
			Vector3 forward = _playerLinks.transform.forward;

			SpawnShadow(remainingDelay, streamBonus, basePosition + right, rotation, manaValue, lastHit, damage, inShadow, shouldSpawnCircularShadow, shouldSpawnStreamShadow, shouldSpawnBlockOfIceShadow, blockOfIceBonusDamage, targetPos, animationHash, normalizedTime, velocityX, velocityZ, startTick, maxTicks, target, blockRemainingDelay);
			SpawnShadow(remainingDelay, streamBonus, basePosition + left, rotation, manaValue, lastHit, damage, inShadow, shouldSpawnCircularShadow, shouldSpawnStreamShadow, shouldSpawnBlockOfIceShadow, blockOfIceBonusDamage, targetPos, animationHash, normalizedTime, velocityX, velocityZ, startTick, maxTicks, target, blockRemainingDelay);
			SpawnShadow(remainingDelay, streamBonus, basePosition + forward, rotation, manaValue, lastHit, damage, inShadow, shouldSpawnCircularShadow, shouldSpawnStreamShadow, shouldSpawnBlockOfIceShadow, blockOfIceBonusDamage, targetPos, animationHash, normalizedTime, velocityX, velocityZ, startTick, maxTicks, target, blockRemainingDelay);
		}
		else
		{
			SpawnShadow(remainingDelay, streamBonus, basePosition, rotation, manaValue, lastHit, damage, inShadow, shouldSpawnCircularShadow, shouldSpawnStreamShadow, shouldSpawnBlockOfIceShadow, blockOfIceBonusDamage, targetPos, animationHash, normalizedTime, velocityX, velocityZ, startTick, maxTicks, target, blockRemainingDelay);
		}

		RpcPlayShotSound();
	}

	private void SpawnShadow(
		float remainingDelay, 
		float streamBonus, 
		Vector3 position, 
		Quaternion rotation, 
		float manaValue, 
		bool lastHit,
		bool damage, 
		bool inShadow, 
		bool shouldSpawnCircularShadow, 
		bool shouldSpawnStreamShadow,
		bool shouldSpawnBlockOfIceShadow,
		float blockOfIceBonusDamage,
		Vector3 targetPos,
		int animationHash, 
		float normalizedTime, 
		float velocityX, 
		float velocityZ, 
		int startTick, 
		int maxTicks, 
		Character target,
		float blockRemainingDelay)
	{
		IceShadowObject shadow = Instantiate(_shadow, position, rotation);

		shadow.InitShadow(_playerLinks, manaValue, streamBonus, lastHit, this);
		shadow.TalentDamage(damage);

		NetworkServer.Spawn(shadow.gameObject);

		RpcSetShadowAnimation(shadow.gameObject, animationHash, normalizedTime, velocityX, velocityZ, rotation);
		RpcInit(shadow.gameObject, manaValue, streamBonus, lastHit, damage, inShadow);

		if (shouldSpawnStreamShadow)
		{
			_icyStreamShadow = shadow.GetComponent<IcyStreamShadow>();
			if (_icyStreamShadow != null)
			{
				_icyStreamShadow.Init(Hero, target, startTick, maxTicks);
				_icyStreamShadow.StartShadowStream();
			}
		}

		if (shouldSpawnBlockOfIceShadow && _blockOfIce != null)
		{
			_blockOfIce.CastFromShadowWithDelay(position, rotation, blockOfIceBonusDamage, targetPos, blockRemainingDelay);
		}

		if (shouldSpawnCircularShadow)
		{
			var shadowFrost = shadow.GetComponent<CircularFrostingShadow>();
			if (shadowFrost != null)
			{
				shadowFrost.Init(Hero, remainingDelay, _circularFrosting.AreaInfo.Radius);
				shadowFrost.StartShadowFrost();
			}
		}
	}

	[ClientRpc]
	private void RpcSetShadowAnimation(GameObject shadowObj, int animationHash, float normalizedTime, float velocityX, float velocityZ, Quaternion rotation)
	{
		if (shadowObj.TryGetComponent(out IceShadowObject shadow))
		{
			shadow.SetAnimationState(animationHash, normalizedTime, velocityX, velocityZ, rotation);
		}
	}

	[ClientRpc]
	private void RpcInit(GameObject obj, float manaValue, float streamBonus,  bool lastHit, bool damage, bool inShadow)
	{
		obj.GetComponent<IceShadowObject>().InitShadow(_playerLinks, manaValue, streamBonus, lastHit, this);
		obj.GetComponent<IceShadowObject>().TalentDamage(damage);
		obj.GetComponent<IceShadowObject>().TalentDamage(inShadow);


	}

	[ClientRpc]
	private void RpcPlayShotSound()
	{
		if (_audioSource != null && audioClip != null) _audioSource.PlayOneShot(audioClip);
	}

    #region Talent

    public void TalentEvade(bool value)
	{
		_talentEvade = value;
	}

	public void TalentDamage(bool value)
	{
		_talentDamage = value;
	}

	public void IceDeathInShadowTalentActive(bool value)
    {
		_iceDeathInShadowTalent = value;
		//AbilityInfoHero.FinalDescription = value ? AbilityInfoHero.Description + $" {text}" : AbilityInfoHero.Description;

	}

    #endregion

    public void Evaded(Skill skill)
	{
		if( _talentEvade) 
		{
			_evaded = true;
			StartCoroutine(CountDownToTalentEvede());
		}
	}
		
	private IEnumerator CountDownToTalentEvede()
	{
		yield return new WaitForSeconds(_evadedTimer);
		_evaded = false;
	}

	protected override bool TryPayCost(List<SkillResourceCost> skillEnergyCosts, bool startCooldown = true)
	{
		if (!IsHaveResourceOnSkill)	return false;

		foreach (var skillCost in skillEnergyCosts)
		{
			if (_evaded && _talentEvade && skillCost.type == ResourceType.Rune) continue;

			var resource = _hero.Resources[skillCost.type];
			resource.CmdUse(Buff.ManaCost.GetBuffedValue(skillCost.value));
		}

		_evaded = false;

		if (startCooldown) Cooldown.SetIncreased(Cooldown.CooldownTime, shouldModify: false);
		TryUseCharge();
		return true;
	}

	public bool IsStreamSkill { get; }
	public bool IsFrostEnergyApplied => true;
}

