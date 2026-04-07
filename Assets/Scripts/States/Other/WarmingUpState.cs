using System.Collections.Generic;
using UnityEngine;

public class WarmingUpState : RefreshingState
{
	private const float BonusPerStack = 0.1f;

	public AbilityForm canceledForm;
	public bool canCancel = false;
	public bool turnOff = false;

	private List<Skill> _affectedSkills = new();
	private SkillManager _skills;

	private List<StatusEffect> _effects = new() { StatusEffect.AbilitySchool };
	public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
	public override States State => States.WarmingUpState;
	public override StateType Type => StateType.Physical;
	public override List<StatusEffect> Effects => _effects;

	public WarmingUpState()
	{
		MaxStacksCount = 3;
		currentStacksCount = 0;
	}


	public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		if (character.TryGetComponent<Character>(out var ability))
		{
			abilities = ability.Abilities;
			abilities.SwitchAvaliable(canceledForm, false);

			foreach (var skill in abilities.Abilities)
			{
				if (skill.Info.AbilityForm == AbilityForm.Physical && skill.AnimTriggerCastPublic != 0)
				{
					_affectedSkills.Add(skill);
					skill.ExtraAnimationSpeedMultiplier = 1 + BonusPerStack * currentStacksCount;
				}
			}
		}
		else
		{
			Debug.LogWarning($"[WarmingUpState] Character {character.name} doesn't have abilities.");
		}

		characterState = character;
		currentStacksCount = 1;
	}

	public override void UpdateState()
	{
		if (turnOff)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		foreach (var skill in _affectedSkills)
		{
			if (skill != null)
			{
				skill.ExtraAnimationSpeedMultiplier = 1;
			}
		}

		if (!characterState.Check(StatusEffect.Ability) && abilities != null)
		{
			abilities.SwitchAvaliable(canceledForm, true);
		}

		currentStacksCount = 0;
		
		characterState.RemoveState(this);
	}

	public override bool Stack(float time)
	{
		duration = time;

		if (currentStacksCount < MaxStacksCount)
		{
			currentStacksCount++;

			foreach (var skill in _affectedSkills)
			{
				if (skill != null)
				{
					skill.ExtraAnimationSpeedMultiplier = 1 + BonusPerStack * currentStacksCount;
				}
			}
		}

		return true;
	}
	
	public override AbstractCharacterState TryApply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		if (!CanEnterState(character)) return null;

		if (currentStacksCount == 0)
			EnterState(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
		else
			Stack(duration);

		return this;
	}
}
