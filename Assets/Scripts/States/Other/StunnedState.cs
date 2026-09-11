using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StunnedStateStacking : RefreshingStateStacking
{

	private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.Ability };
	public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
	public override States State => States.Stun;
	public override StateType Type => StateType.Physical;
	public override List<StatusEffect> Effects => _effects;

	private float _maxDuration = 4f;


    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		SetMaxStacks(1);
		currentStacksCount = 1;
		RemainingDuration = Mathf.Min(durationToExit, _maxDuration);
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
		if (RemainingDuration <= 0)
		{
			ExitState();
		}
	}
	
	

	public override bool Stack(float newDuration)
	{
		if (newDuration > RemainingDuration)
		{
			RemainingDuration = newDuration -RemainingDuration;
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
