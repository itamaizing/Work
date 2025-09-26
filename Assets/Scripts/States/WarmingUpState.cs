using System.Collections.Generic;
using UnityEngine;

public class WarmingUpState : AbstractCharacterState
{
	private float _duration;
	private float _baseDuration;
	private int _currentStacks = 1;

	private const int MaxStacks = 3;
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

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		_characterState = character;
		_duration = durationToExit;
		_baseDuration = durationToExit;

		if (character.TryGetComponent<Character>(out var ability))
		{
			_abilities = ability.Abilities;
			_abilities.SwitchAvaliable(canceledForm, false);

			foreach (var skill in _abilities.Abilities)
			{
				if (skill.AbilityForm == AbilityForm.Physical && skill.AnimTriggerCastPublic != 0)
				{
					skill.ExtraAnimationSpeedMultiplier *= (1 + BonusPerStack);
					_affectedSkills.Add(skill);
				}
			}
		}
		else
		{
			Debug.LogWarning($"[WarmingUpState] Character {character.name} doesn't have abilities.");
		}
	}

	public override void UpdateState()
	{
		_duration -= Time.deltaTime;

		if (_duration <= 0f || turnOff)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		_characterState.RemoveState(this);

		foreach (var skill in _affectedSkills)
		{
			if (skill != null)
			{
				skill.ExtraAnimationSpeedMultiplier /= (1 + BonusPerStack * _currentStacks);
			}
		}

		if (!_characterState.Check(StatusEffect.Ability) && _abilities != null)
		{
			_abilities.SwitchAvaliable(canceledForm, true);
		}
	}

	public override bool Stack(float time)
	{
		_duration = time;

		if (_currentStacks < MaxStacks)
		{
			_currentStacks++;

			foreach (var skill in _affectedSkills)
			{
				if (skill != null)
				{
					skill.ExtraAnimationSpeedMultiplier *= (1 + BonusPerStack);
				}
			}
		}

		return true;
	}
}
