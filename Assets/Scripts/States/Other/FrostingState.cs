using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrostingState : AbstractCharacterState
{
	public bool turnOff = false;

	private GameObject _ice;
	private AudioSource _audioSource;
	//private float _duration;
	private float _baseDuration;
	private float _damageOnStart;
	private float _damageToExit;

	private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.AbilitySpeed };
	public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
	public override States State => States.Frosting;
	public override StateType Type => StateType.Magic;
	public override List<StatusEffect> Effects => _effects;

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		//Debug.Log("Entering Frosting State");
		characterState = character;

		if (damageToExit == 0)
		{
			_damageToExit = 10000;
		}
		else
		{
			_damageToExit = damageToExit;
		}
		duration = durationToExit;
		_baseDuration = durationToExit;
		_audioSource = character.GetComponent<AudioSource>();

		_damageOnStart = characterState.Character.Health.SumDamageTaken;
		characterState.Character.Move.CanMove = false;
		characterState.Character.Move.LookAtTransform(characterState.gameObject.transform);

		if (character.TryGetComponent<Character>(out var ability))
		{
			abilities = ability.Abilities;

			foreach (var abil in abilities.Abilities)
			{
				if (abil.AbilityForm == AbilityForm.Physical)
				{
					abil.Buff.CastSpeed.ReductionPercentage(.5f);
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

	public override void UpdateState()
	{
		duration -= Time.deltaTime;
		if (characterState.Character.Health.SumDamageTaken - _damageOnStart >= _damageToExit || duration < 0 || turnOff)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		//Debug.Log("Exiting Frosting State");
		characterState.RemoveState(this);

		if (!characterState.Check(StatusEffect.Move))
		{
			characterState.Character.Move.CanMove = true;
		}

		characterState.Character.Move.StopLookAt();

		if (!characterState.Check(StatusEffect.AbilitySpeed) && abilities != null)
		{
			foreach (var abil in abilities.Abilities)
			{
				if (abil.AbilityForm == AbilityForm.Physical)
				{
					abil.Buff.CastSpeed.IncreasePercentage(.5f);
				}
			}
		}

		if (characterState.StateEffects.Ice != null) _ice.SetActive(false);
	}

	public override bool Stack(float time)
	{
		duration = _baseDuration;
		return true;
	}

}
