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


    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		MaxStacksCount = 1;
		currentStacksCount = 1;
		
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
	}

	public override bool Stack(float time)
	{
		duration += time;
		Debug.LogError("new duration: " + duration);
		return true;
	}

	public override void ExitState()
	{
		 characterState.Character.Move.IsMoveBlocked = false;
		abilities.SetAbilitiesDisactive(false);
		characterState.RemoveState(this);
	}
}
