using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IceShadow : Skill
{
	[Header("Ability properties")]
	[SerializeField] private IceShadowObject _shadow;
	[SerializeField] private IcyStream _icyStream;
	[SerializeField] private CircularFrosting _circularFrosting;
	[ReadOnly][SerializeField] private IcyStreamShadow _icyStreamShadow;
	[SerializeField] private HeroComponent _playerLinks; 
	[SerializeField] private SeriesOfStrikes _combo;
	[SerializeField] private AudioClip audioClip;
	//[SerializeField] private bool isTest = true;

	private IcyStream.IcyStreamState? _capturedState;

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

        //_energy = (Energy)_playerLinks.Resources[ResourceType.Energy];
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

		if (_circularFrosting != null)
		{
			_remainingDelayCircularFrostin = _circularFrosting.RemainingDelay;
			_circularFrosting.TryCancel(true);
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
		
	}

	private void Shoot()
	{
		/*IceShadowObject projectileGm = Instantiate(_shadow, gameObject.transform.position, Quaternion.identity);
		projectileGm.Init(_playerLinks.gameObject ,Mana.Value);*/
		_lastHit = _combo.MakeHit(null, Info.AbilityForm, 1, _manaUsed, 0, _combo.GetMultipliedSpeed() / SpeedScaleDivisor);

		_manaUsed = Mathf.Min(_energy.CurrentValue, MaxManaPerCast);
		_energy.CmdUse(_manaUsed);

		CmdCreateProjecttile(0, _manaUsed, _lastHit, _talentDamage,	_iceDeathInShadowTalent, _capturedState?.CurrentTick ?? -1, _capturedState?.MaxTicks ?? -1, _capturedState?.Target != null ? _capturedState.Value.Target.netIdentity : null);
	}

	private void SpawnShadow(Vector3 position, Quaternion rotation, float manaValue, bool lastHit, bool damage, bool inShadow, int animationHash, float normalizedTime, float velocityX, float velocityZ, int startTick, int maxTicks, Character target)
	{
		IceShadowObject shadow = Instantiate(_shadow, position, rotation);

		shadow.Init(_playerLinks, manaValue, lastHit, this);
		shadow.TalentDamage(damage);

		NetworkServer.Spawn(shadow.gameObject);

		RpcSetShadowAnimation(shadow.gameObject, animationHash, normalizedTime, velocityX, velocityZ, rotation);
		RpcInit(shadow.gameObject, manaValue, lastHit, damage, inShadow);

		_icyStreamShadow = shadow.GetComponent<IcyStreamShadow>();

		if (_icyStreamShadow != null && target != null && startTick > 0 && maxTicks > 0)
		{
			_icyStreamShadow.Init(Hero, target, startTick, maxTicks);
			_icyStreamShadow.StartShadowStream();
		}

		var shadowFrost = shadow.GetComponent<CircularFrostingShadow>();
		if (shadowFrost == null) return;

		shadowFrost.Init(Hero, _remainingDelayCircularFrostin, _circularFrosting.AreaInfo.Radius);
		shadowFrost.StartShadowLogic();
	}

	[Command]
	private void CmdCreateProjecttile(float angle, float manaValue, bool lastHit, bool damage, bool inShadow, int startTick, int maxTicks, NetworkIdentity targetIdentity)
	{
		AnimatorStateInfo stateInfo = _playerLinks.Animator.GetCurrentAnimatorStateInfo(0);
		int animationHash = stateInfo.fullPathHash;
		float normalizedTime = stateInfo.normalizedTime % 1f;
		float velocityX = _playerLinks.Animator.GetFloat(HashAnimPlayer.VelocityX);
		float velocityZ = _playerLinks.Animator.GetFloat(HashAnimPlayer.VelocityZ);
		Quaternion rotation = _playerLinks.transform.rotation;

		Vector3 basePosition = _playerLinks.transform.position;

		Character target = null;
		if (targetIdentity != null)
			target = targetIdentity.GetComponent<Character>();

		if (lastHit)
		{
			Vector3 right = _playerLinks.transform.right;
			Vector3 left = -_playerLinks.transform.right;
			Vector3 forward = _playerLinks.transform.forward;

			SpawnShadow(basePosition + right, rotation, manaValue, lastHit, damage, inShadow, animationHash, normalizedTime, velocityX, velocityZ, startTick, maxTicks, target);
			SpawnShadow(basePosition + left, rotation, manaValue, lastHit, damage, inShadow, animationHash, normalizedTime, velocityX, velocityZ, startTick, maxTicks, target);
			SpawnShadow(basePosition + forward, rotation, manaValue, lastHit, damage, inShadow, animationHash, normalizedTime, velocityX, velocityZ, startTick, maxTicks, target);
		}

		else
		{
			SpawnShadow(basePosition, rotation, manaValue, lastHit, damage, inShadow, animationHash, normalizedTime, velocityX, velocityZ, startTick, maxTicks, target);
		}

		RpcPlayShotSound();
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
	private void RpcInit(GameObject obj, float manaValue, bool lastHit, bool damage, bool inShadow)
	{
		obj.GetComponent<IceShadowObject>().Init(_playerLinks, manaValue, lastHit, this);
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

    public void Evaded()
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
		if (IsHaveResourceOnSkill)
		{
			if (_evaded && _talentEvade)
			{
				/*foreach (var skillCost in _skillEnergyCosts)
				{
					var resource = _hero.Resources.First(r => r.Type == skillCost.type);
					resource.CmdUse(Buff.ManaCost.GetBuffedValue(skillCost.value));
				}*/
				_evaded = false;
			}
			else
			{
				foreach (var skillCost in _skillEnergyCosts)
				{
					var resource = _hero.Resources[skillCost.type];
					resource.CmdUse(Buff.ManaCost.GetBuffedValue(skillCost.value));
				}
				_evaded = false;
			}

			if (startCooldown)
			{
				Cooldown.SetIncreased(Cooldown.CooldownTime, shouldModify: false);
			}

			TryUseCharge();
			return true;
		}
		else return false;
	}
}

