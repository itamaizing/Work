using System.Collections.Generic;
using UnityEngine;

public class WarmingUpState : AbstractCharacterState
{
	private const float BonusPerStack = 1f;

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
		currentStacksCount = 1;
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
		characterState.RemoveState(this);

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

		currentStacksCount = 1;
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
}
