using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilitySchoolDebuff : RefreshingState
{
	public bool turnOff = false;
	private float _baseDuration;
	public Schools canceledSchoool;
	
	private Character _character;

	private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.AbilitySchool };
	public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
	public override States State => States.SchoolDebuff;
	public override StateType Type => StateType.Immaterial;
	public override List<StatusEffect> Effects => _effects;

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		characterState = character;
		
		_character = character.GetComponent<Character>();
	}

	public override void UpdateState() { }

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
		duration = time;
		return true;
	}
	public override AbstractCharacterState TryApply(CharacterState character, float durationToExit, float damageToExit,
		Character personWhoMadeBuff, string skillName)
	{
		if (!CanEnterState(character)) return null;

		MaxStacksCount = 1;
		
		BaseInit(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);

		TryCancel(character);

		if (currentStacksCount == 0)
		{
			EnterState(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
			currentStacksCount = 1;
		}
		else
		{
			Stack(durationToExit);
		}

		return this;
	}
	
	private void TryCancel(CharacterState character)
	{
		var targetAbilities = character.Character?.Abilities;
		if (targetAbilities?.CurrentCastingSkill == null) return;

		var school = targetAbilities.CurrentCastingSkill.Info.School;

		targetAbilities.CurrentCastingSkill.CmdCancelActiveSkill();

		if (school != Schools.None)
		{
			canceledSchoool = school;
			if (character.TryGetComponent<Character>(out var ability))
			{
				abilities = ability.Abilities;
				abilities.SwitchAvaliable(canceledSchoool, false);
			}
		}
	}
}
