using System.Collections.Generic;
using UnityEngine;

public class WarmingUpState : RefreshingState
{
	private const float BonusPerStack = 0.1f;
	private float _savedRegenValue = 0f;
	private const float RegenMultiplier = 2f;
	
	private AttributeModifier _incomingHealModifier = new AttributeModifier(0.1f, ModifierType.Percent);

	private List<Skill> _affectedSkills = new();
	private SkillManager _skills;
	public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
	public override States State => States.WarmingUpState;
	public override StateType Type => StateType.Physical;
	public override List<StatusEffect> Effects => new List<StatusEffect>() { StatusEffect.Strengthening };

	private float baseDuration;

	public WarmingUpState()
	{
		MaxStacksCount = 3;
		currentStacksCount = 0;
	}
	
	public override void EnterState(CharacterState character, float durationToExit, float damageToExit,
		Character personWhoMadeBuff, string skillName)
	{
		baseDuration = durationToExit;
		currentStacksCount = 1;
		foreach (var skill in abilities.Abilities)
		{
			if (skill.Info.AbilityForm == AbilityForm.Physical && skill.AnimTriggerCastPublic != 0)
			{
				_affectedSkills.Add(skill);
				skill.ExtraAnimationSpeedMultiplier = 1 + BonusPerStack * currentStacksCount;
			}
		}

		var health = character.Character.Health;
		_savedRegenValue = health.RegenerationValue;
		if (skillName.Contains("HealingIncrease") && character.isServer)
		{
			health.RegenerationValue = _savedRegenValue * RegenMultiplier;
			health.AddIncomingModifier(_incomingHealModifier);
		}
	}

	public override void UpdateState()
	{
	}
	
	public override void ExitState()
	{
		currentStacksCount = 0;
		characterState.RemoveState(this);
		
		ModifyAnimationSpeed(0);

		if (characterState.isServer && _savedRegenValue > 0f)
		{
			characterState.Character.Health.RegenerationValue = _savedRegenValue;
			characterState.Character.Health.RemoveIncomingModifier(_incomingHealModifier);
			_savedRegenValue = 0f;
		}

	}
	
	public override bool Stack(float time)
	{
		duration = time;
		if (currentStacksCount < MaxStacksCount)
		{
			currentStacksCount++;
			ModifyAnimationSpeed(currentStacksCount);
			return true;
		}

		return false;
	}
	public override void ReduceStack()
	{
		currentStacksCount--;

		if (currentStacksCount <= 0)
		{
			characterState.StateIcons.RemoveItemByState(State);
			ExitState();
		}
		else
		{
			characterState.StateIcons.ActivateIco(State, baseDuration, -1, true, MaxStacksCount);
			duration = baseDuration;
			ModifyAnimationSpeed(currentStacksCount);
		}
	}

	private void ModifyAnimationSpeed(int currentStacks)
	{
		foreach (var skill in _affectedSkills)
		{
			if (skill != null)
			{
				skill.ExtraAnimationSpeedMultiplier = 1 + BonusPerStack * currentStacks;
			}
		}
	}
	
	public override AbstractCharacterState TryApply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		if (!CanEnterState(character)) return null;

		BaseInit(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);

		if (currentStacksCount == 0)
			EnterState(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
		else
			Stack(durationToExit);

		return this;
	}
}
