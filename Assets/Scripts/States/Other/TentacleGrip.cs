using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TentacleGrip : AbstractCharacterState
{
	public bool turnOff = false;
	//private PlayerAbilities _abilities;
	private float _baseDuration;

	private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.Ability };
	public override BaffDebaff BaffDebaff => BaffDebaff.Null;
	public override States State => States.TentacleGrip;
	public override StateType Type => StateType.Immaterial;
	public override List<StatusEffect> Effects => _effects;


    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		if (character.TryGetComponent<Character>(out var ability))
		{
			abilities = ability.Abilities;
			abilities.SetPhysicalAbilitiesDisactive(true);
		}
		else Debug.Log("no ability at " + character.gameObject.name);

		characterState.Character.Move.IsMoveBlocked = true;
		characterState.Character.Move.StopMoveAndAnimationMove();

		_baseDuration = durationToExit;
	}

	public override void UpdateState()
	{

	}

	public override void ExitState()
	{
		characterState.RemoveState(this);
		if (!characterState.Check(StatusEffect.Move)) characterState.Character.Move.IsMoveBlocked = false;
		if (!characterState.Check(StatusEffect.Ability) && abilities != null) abilities.SetPhysicalAbilitiesDisactive(false);
	}

	public override bool Stack(float time)
	{
		if (_baseDuration > time) return false;

		else
		{
			duration = time;
			return true;
		}
	}
}

