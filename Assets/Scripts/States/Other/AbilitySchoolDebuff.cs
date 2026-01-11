using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilitySchoolDebuff : AbstractCharacterState
{
	public bool turnOff = false;
	private float _baseDuration;
	private float _duration;
	public Schools canceledSchoool;

	private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.AbilitySchool };
	public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
	public override States State => States.SchoolDebuff;
	public override StateType Type => StateType.Immaterial;
	public override List<StatusEffect> Effects => _effects;

  

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		characterState = character;

		if (character.TryGetComponent<Character>(out var ability))
		{
			abilities = ability.Abilities;
			abilities.SwitchAvaliable(canceledSchoool, false);
		}
		else
		{
			Debug.Log("no ability at " + character.gameObject.name);
		}
		_duration = durationToExit;
		_baseDuration = durationToExit;
	}

	public override void UpdateState()
	{
		_duration -= Time.deltaTime;
		if (_duration < 0 || turnOff)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		characterState.RemoveState(this);
		if (!characterState.Check(StatusEffect.Ability) && abilities != null)
		{
			abilities.SwitchAvaliable(canceledSchoool, true);
		}
	}

	public override bool Stack(float time)
	{
		if (_duration > time)
		{
			return true;
		}
		else
		{
			_duration = time;
			return true;
		}
	}
}
