using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StunnedState : RefreshingState
{

	private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.Ability };
	public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
	public override States State => States.Stun;
	public override StateType Type => StateType.Physical;
	public override List<StatusEffect> Effects => _effects;

	private float _maxDuration = 4f;


    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		MaxStacksCount = 1;
		currentStacksCount = 1;
		duration = Mathf.Min(durationToExit, _maxDuration);
		if (character.TryGetComponent<Character>(out var ability))
		{
			abilities = ability.Abilities;
			abilities.SetAbilitiesDisactive(true);
		}
		else Debug.Log("no ability at " + character.gameObject.name);

		characterState.Character.Move.IsMoveBlocked = true;
		characterState.Character.Move.StopMoveAndAnimationMove();
	}

	public override void UpdateState()
	{
		if (duration <= 0)
		{
			ExitState();
		}
	}
	
	public override AbstractCharacterState TryApply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		if (!CanEnterState(character)) return null;

		BaseInit(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);

		if (currentStacksCount == 0)
			EnterState(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
		else
			Stack(duration);

		return this;
	}

	public override bool Stack(float newDuration)
	{
		if (newDuration > duration)
		{
			duration = newDuration - duration;
		}
		return true;
	}

	public override void ExitState()
	{
		currentStacksCount = 0;
		 characterState.Character.Move.IsMoveBlocked = false;
		abilities.SetAbilitiesDisactive(false);
		characterState.RemoveState(this);
	}
}
