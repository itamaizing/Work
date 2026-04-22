using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FrostingState : AbstractCharacterState
{
	public bool turnOff = false;

	private GameObject _ice;
	private AudioSource _audioSource;
	private NinjaResources _ninjaResources;
	private TalentSystem _talentSystem;
	private float _duration;
	private float _baseDuration;
	private float _damageOnStart;
	private float _damageToExit;

	private bool _isFrostTalentActive;

	private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.AbilitySpeed };
	public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
	public override States State => States.Frosting;
	public override StateType Type => StateType.Magic;
	public override List<StatusEffect> Effects => _effects;

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		//Debug.Log("Entering Frosting State");
		if (damageToExit == 0)
		{
			_damageToExit = 30;
		}
		else
		{
			_damageToExit = damageToExit;
		}
		_duration = durationToExit;
		_baseDuration = durationToExit;
		_audioSource = character.GetComponent<AudioSource>();
		if (personWhoMadeBuff.TryGetComponent<NinjaResources>(out NinjaResources resources)) _ninjaResources = resources;
		if (personWhoMadeBuff.TryGetComponent<TalentSystem>(out TalentSystem talentSystem)) _talentSystem = talentSystem;

		if (_talentSystem != null) _isFrostTalentActive = _talentSystem.ActiveTalents.Any(t => t.GetType().Name == "FrostTalent_12");

		_damageOnStart = characterState.Character.Health.SumDamageTaken;
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

	public override void UpdateState()
	{
		bool timeExpired = _duration < 0;
		bool damageExceeded = characterState.Character.Health.SumDamageTaken - _damageOnStart >= _damageToExit;

		if (damageExceeded || turnOff)
		{
			ExitState();
			return;
		}

		if (timeExpired)
		{
			if (_isFrostTalentActive)
			{
				RestartFrosting();
				return;
			}

			ExitState();
		}
	}

	public override void ExitState()
	{
		//Debug.Log("Exiting Frosting State");
		characterState.RemoveState(this);

		if (!characterState.Check(StatusEffect.Move))
		{
			characterState.Character.Move.SetCanMoveState(true);
		}

		characterState.Character.Move.StopLookAt();

		//if (!characterState.Check(StatusEffect.AbilitySpeed) && abilities != null)
		//{
		//	foreach (var abil in abilities.Abilities)
		//	{
		//		if (abil.Info.AbilityForm == AbilityForm.Physical)
		//		{
		//			abil.Buff.CastSpeed.IncreasePercentage(.5f);
		//		}
		//	}
		//}

		if (characterState.StateEffects.Ice != null) _ice.SetActive(false);
	}

	public override bool Stack(float time)
	{
		_duration = _baseDuration;
		_damageOnStart = characterState.Character.Health.SumDamageTaken;

		if (_damageToExit < 30) _damageToExit = 30;

		if (_ninjaResources != null && _ninjaResources.IsRepeatedFrost)	AddFrozenCmd();

		return true;
	}

	private void RestartFrosting()
	{
		_duration = _baseDuration;
	}

	[Command] private void AddFrozenCmd() => AddFrozenRpc();
	[ClientRpc] private void AddFrozenRpc() => characterState.AddStateLogic(States.Frozen, _baseDuration, 0f, Schools.None, characterState.Character.gameObject, "RepeatedFrost");
}
