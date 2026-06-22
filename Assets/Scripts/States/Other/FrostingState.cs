using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FrostingState : RefreshingState
{
	public bool turnOff = false;

	private GameObject _ice;
	private AudioSource _audioSource;
	private NinjaResources _ninjaResources;
	private float _baseDuration;

	private float _deepFrostDurability = 30f;
	private float _damageCount = 0f;

	private bool _isFrostTalentActive;

	private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.AbilitySpeed };
	public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
	public override States State => States.Frosting;
	public override StateType Type => StateType.Magic;
	public override List<StatusEffect> Effects => _effects;

	public string SkillName = "";

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		currentStacksCount = 1;
		_damageCount = 0;

		this.damageToExit = damageToExit == 0 ? 1 : damageToExit;

		if (_ninjaResources.IsDeepFrosting)
		{
			this.damageToExit = _deepFrostDurability;
		}
		
		duration = durationToExit;
		_baseDuration = durationToExit;
		_audioSource = character.GetComponent<AudioSource>();

		characterState.Character.Move.SetCanMoveState(false);
		characterState.Character.Move.LookAtTransform(characterState.gameObject.transform);

		if (character.TryGetComponent<Character>(out var ability))
		{
			abilities = ability.Abilities;

			foreach (var abil in abilities.Abilities)
			{
				if (abil.Info.AbilityForm == AbilityForm.Physical)
				{
					abil.Buff.CastSpeed.ReductionPercentage(.05f);
				}
			}
		}

		else
		{
			Debug.Log("no ability at " + character.gameObject.name);
		}

		if (characterState.StateEffects.Ice != null)
		{
			_ice = characterState.StateEffects.Ice;
			_ice.SetActive(true);
		}

		if (characterState.StateEffects.FrostingAudio != null) _audioSource.PlayOneShot(characterState.StateEffects.FrostingAudio);
	}

	private void SubscribeOnDamage()
	{
		characterState.Character.Health.DamageTaken += OnDamaged;
		characterState.Character.Health.OnBeforeTakeDamage += OnDamaged;
	}

	private void UnSubscribeOnDamage()
	{
		characterState.Character.Health.DamageTaken -= OnDamaged;
		characterState.Character.Health.OnBeforeTakeDamage -= OnDamaged;
	}

	private void OnDamaged(Damage damage, Skill ability)
	{
		_damageCount += damage.Value;
		if(_damageCount > damageToExit)
			ExitState();
	}

	public override void UpdateState()
	{
	}

	public override void ExitState()
	{
		UnSubscribeOnDamage();
		_damageCount = 0;
		//Debug.Log("Exiting Frosting State");
		characterState.RemoveState(this);
		currentStacksCount = 0;
		if (!characterState.Check(StatusEffect.Move))
		{
			characterState.Character.Move.SetCanMoveState(true);
		}

		characterState.Character.Move.StopLookAt();

		if (characterState.StateEffects.Ice != null) _ice.SetActive(false);
	}

	public override bool Stack(float time)
	{
		duration = Mathf.Max(duration, time);

		if (_ninjaResources != null && _ninjaResources.IsRepeatedFrost)
		{
			currentStacksCount = 0;
			if(characterState.isClient)
				_ninjaResources.AddRepeatedFrozen(characterState.gameObject,_baseDuration);
		}

		return true;
	}

	public override AbstractCharacterState TryApply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		if (!CanEnterState(character)) return null;

		BaseInit(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
		SkillName = skillName;
		
		if(!_ninjaResources)
			if (personWhoMadeBuff.TryGetComponent<NinjaResources>(out NinjaResources resources)) _ninjaResources = resources;

		UnSubscribeOnDamage();
		SubscribeOnDamage();
		
		if (currentStacksCount == 0)
		{
			EnterState(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
		}
		else
			Stack(durationToExit);

		return this;
	}
}
