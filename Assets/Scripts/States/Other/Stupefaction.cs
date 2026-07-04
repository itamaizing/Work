using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stupefaction : AbstractCharacterState
{
	public bool turnOff = false;
	//private PlayerAbilities _abilities;
	private float _baseDuration;

	private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.Ability };
	public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
	public override States State => States.Stupefaction;
	public override StateType Type => StateType.Physical;
	public override List<StatusEffect> Effects => _effects;


    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		characterState = character;

		if (character.TryGetComponent<Character>(out var ability))
		{
			abilities = ability.Abilities;
			abilities.SetAbilitiesDisactive(true);
		}

		characterState.Character.Move.IsMoveBlocked = true;
		characterState.Character.Move.StopMoveAndAnimationMove();

		_baseDuration = durationToExit;

		characterState.Character.Health.DamageTaken += OnAnyDamage;
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
		characterState.Character.Health.DamageTaken -= OnAnyDamage;
		characterState.RemoveState(this);
		if (!characterState.Check(StatusEffect.Move)) characterState.Character.Move.IsMoveBlocked = false;
		if (!characterState.Check(StatusEffect.Ability) && abilities != null) abilities.SetAbilitiesDisactive(false);
		turnOff = false;
	}

	public override bool Stack(float time)
	{
		if (_baseDuration > time)
		{
			return false;
		}
		else
		{
			duration = time;
			return true;
		}
	}

	private void OnAnyDamage(Damage damage, Skill fromSkill) => turnOff = true;

}
