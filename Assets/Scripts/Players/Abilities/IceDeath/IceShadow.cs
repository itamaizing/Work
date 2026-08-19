using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct ShadowCastContext
{
	public float RemainingDelay;
	public float ManaValue;
	public float StreamBonus;
	public bool LastHit;
	public bool TalentDamage;
	public bool InShadow;
	public bool SpawnCircularShadow;
	public bool SpawnStreamShadow;
	public bool SpawnBlockOfIceShadow;
	public float BlockOfIceBonusDamage;
	public Vector3 TargetPos;
	public int StartTick;
	public int MaxTicks;
	public float BlockRemainingDelay;
}

public struct AnimSnapshot
{
	public int AnimationHash;
	public float NormalizedTime;
	public float VelocityX;
	public float VelocityZ;
}

public class IceShadow : Skill, IEnergyDamagable, SkillQueue.IPreemptsQueue
{
	[Header("Ability properties")]
	[SerializeField] private IceShadowObject _shadow;
	[SerializeField] private IcyStream _icyStream;
	[SerializeField] private BlockOfIce _blockOfIce;
	[SerializeField] private CircularFrosting _circularFrosting;
	[SerializeField] private HeroComponent _playerLinks;
	[SerializeField] private AudioClip audioClip;

	private IcyStream.IcyStreamState? _capturedState;
	private BlockOfIce.BlockOfIceState? _capturedBlockOfIceState; 

	private AudioSource _audioSource;
	private Energy _energy;
	private bool _lastHit = false;
	private bool _isEndCasting;
	private bool _talentEvade = false;
	private bool _talentDamage = false;
	private bool _iceDeathInShadowTalent = false;
	private bool _evaded = false;
	private float _evadedTimer = 2f;
	private float _manaUsed = 0;

	#region Const
	private const float MaxManaPerCast = 30f;
	#endregion

	protected override bool IsCanCast => IsCanCastCheck();

	protected override int AnimTriggerCastDelay => 0;

	protected override int AnimTriggerCast => 0;
    
	private enum CapturedShadowSkill { None, IcyStream, BlockOfIce, CircularFrosting }
	private CapturedShadowSkill _captured = CapturedShadowSkill.None;

	private bool IsCanCastCheck() => true;

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
    
	public bool TryPreemptCurrentCast(Skill busySkill)
	{
		if (!_isEndCasting) return false;
		if (busySkill == null) return false;

		if (busySkill == _circularFrosting)
		{
			if ((_icyStream != null && _icyStream.IsCasting) || (_blockOfIce != null && _blockOfIce.IsCasting))
			{
				return false;
			}

			_circularFrosting.TryCancel(true);
			_captured = CapturedShadowSkill.CircularFrosting;
			return true;
		}

		if (busySkill == _icyStream)
		{
			if (_icyStream.TryGetState(out var state))
			{
				_capturedState = state;
			}

			_icyStream.StopStream();
			_icyStream.TryCancel(true);
			_captured = CapturedShadowSkill.IcyStream;
			return true;
		}

		if (busySkill == _blockOfIce)
		{
			if (_blockOfIce.TryGetState(out var blockState))
			{
				_capturedBlockOfIceState = blockState;
			}

			_blockOfIce.TryCancel(true);
			_captured = CapturedShadowSkill.BlockOfIce;
			return true;
		}

		return false;
	}

	protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
	{
		if (_energy == null) _energy = (Energy)Hero.Resources[ResourceType.Energy];

		while (_circularFrosting != null && _circularFrosting.IsCasting && (_icyStream.IsCasting || _blockOfIce.IsCasting))
		{
			yield return null;
		}

		if (_captured == CapturedShadowSkill.None)
		{
			if (_icyStream != null && _icyStream.IsCasting)
			{
				if (_icyStream.TryGetState(out var streamState))
				{
					_capturedState = streamState;
				}

				_captured = CapturedShadowSkill.IcyStream;
				_icyStream.StopStream();
				_icyStream.TryCancel(true);
			}
			else if (_blockOfIce != null && _blockOfIce.IsCasting)
			{
				if (_blockOfIce.TryGetState(out var blockState))
				{
					_capturedBlockOfIceState = blockState;
				}

				_captured = CapturedShadowSkill.BlockOfIce;
				_blockOfIce.TryCancel(true);
			}
		}

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
		_captured = CapturedShadowSkill.None;
	}

	private void Shoot()
	{
		bool triggeredFromStream = _captured == CapturedShadowSkill.IcyStream;
		bool triggeredFromBlockOfIce = _captured == CapturedShadowSkill.BlockOfIce;
		bool triggeredFromFrosting = _captured == CapturedShadowSkill.CircularFrosting;
		bool triggeredFromOtherSkill = _captured != CapturedShadowSkill.None;

		if (!triggeredFromOtherSkill)
		{
			_manaUsed = Mathf.Min(_energy.CurrentValue, MaxManaPerCast);
			_energy.CmdUse(_manaUsed);
		}

		float bonusDuration = 0f;
		if (triggeredFromFrosting) bonusDuration += _circularFrosting.RemainingDelay;
		if (_capturedState.HasValue)
		{
			int remainingTicks = _capturedState.Value.MaxTicks - _capturedState.Value.CurrentTick;
			bonusDuration += remainingTicks * 0.3f;
		}

		Character character = Targeting?.Target?.Character;
		Vector3 targetPos = _capturedBlockOfIceState.HasValue
			? _capturedBlockOfIceState.Value.TargetPosition
			: (character != null ? character.transform.position : Vector3.zero);

		var ctx = new ShadowCastContext
		{
			RemainingDelay = triggeredFromFrosting ? _circularFrosting.RemainingDelay : 0f,
			ManaValue = _manaUsed,
			StreamBonus = bonusDuration,
			LastHit = _lastHit,
			TalentDamage = _talentDamage,
			InShadow = _iceDeathInShadowTalent,
			SpawnCircularShadow = triggeredFromFrosting,
			SpawnStreamShadow = triggeredFromStream,
			SpawnBlockOfIceShadow = triggeredFromBlockOfIce,
			BlockOfIceBonusDamage = _capturedBlockOfIceState?.BonusDamage ?? 0f,
			TargetPos = targetPos,
			StartTick = _capturedState?.CurrentTick ?? 0,
			MaxTicks = _capturedState?.MaxTicks ?? 0,
			BlockRemainingDelay = triggeredFromBlockOfIce ? _blockOfIce.RemainingCastDelay : 0f
		};

		CmdCreateProjecttile(ctx, character);

		_capturedBlockOfIceState = null;
	}

	[Command]
	private void CmdCreateProjecttile(ShadowCastContext ctx, Character targetIdentity)
	{
		var anim = new AnimSnapshot
		{
			AnimationHash = _playerLinks.Animator.GetCurrentAnimatorStateInfo(0).fullPathHash,
			NormalizedTime = _playerLinks.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1f,
			VelocityX = _playerLinks.Animator.GetFloat(HashAnimPlayer.VelocityX),
			VelocityZ = _playerLinks.Animator.GetFloat(HashAnimPlayer.VelocityZ)
		};

		Quaternion rotation = _playerLinks.transform.rotation;
		Vector3 basePosition = _playerLinks.transform.position;

		if (ctx.SpawnCircularShadow) _circularFrosting.PayEnergyOnInterruptedDelay();

		Character target = targetIdentity;

		if (ctx.LastHit)
		{
			SpawnShadow(ctx, basePosition + _playerLinks.transform.right, rotation, anim, target);
			SpawnShadow(ctx, basePosition - _playerLinks.transform.right, rotation, anim, target);
			SpawnShadow(ctx, basePosition + _playerLinks.transform.forward, rotation, anim, target);
		}
		else
		{
			SpawnShadow(ctx, basePosition, rotation, anim, target);
		}

		RpcPlayShotSound();
	}

	private void SpawnShadow(ShadowCastContext ctx, Vector3 position, Quaternion rotation, AnimSnapshot anim, Character target)
	{
		IceShadowObject shadow = Instantiate(_shadow, position, rotation);

		shadow.InitShadow(_playerLinks, ctx.ManaValue, ctx.StreamBonus, ctx.LastHit, this);
		shadow.TalentDamage(ctx.TalentDamage);

		NetworkServer.Spawn(shadow.gameObject);

		RpcSetShadowAnimation(shadow.gameObject, anim, rotation);
		RpcInit(shadow.gameObject, ctx);

		if (ctx.SpawnStreamShadow && _icyStream != null)
		{
			_icyStream.TriggerShadowStream(position, rotation, ctx.StartTick, ctx.MaxTicks);
		}

		if (ctx.SpawnBlockOfIceShadow && _blockOfIce != null)
		{
			_blockOfIce.CastFromShadowWithDelay(position, rotation, ctx.BlockOfIceBonusDamage, ctx.TargetPos, ctx.BlockRemainingDelay);
		}

		if (ctx.SpawnCircularShadow && _circularFrosting != null)
		{
			_circularFrosting.TriggerDelayedFrosting(ctx.RemainingDelay, position);
		}
	}

	[ClientRpc]
	private void RpcSetShadowAnimation(GameObject shadowObj, AnimSnapshot anim, Quaternion rotation)
	{
		if (shadowObj.TryGetComponent(out IceShadowObject shadow))
			shadow.SetAnimationState(anim.AnimationHash, anim.NormalizedTime, anim.VelocityX, anim.VelocityZ, rotation);
	}

	[ClientRpc]
	private void RpcInit(GameObject obj, ShadowCastContext ctx)
	{
		var shadowObj = obj.GetComponent<IceShadowObject>();
		shadowObj.InitShadow(_playerLinks, ctx.ManaValue, ctx.StreamBonus, ctx.LastHit, this);
		shadowObj.TalentDamage(ctx.TalentDamage);
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
	}
	
	public void EndCastTalent(bool value)
	{
		if (_isEndCasting == value) return;
		_isEndCasting = value;
	}

	#endregion

	public void Evaded(Skill skill)
	{
		if (_talentEvade) 
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
		if (!IsHaveResourceOnSkill) return false;

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