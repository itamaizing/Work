using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StunnedState : RefreshingState
{
	public bool turnOff = false;
	private float _baseDuration;

	private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.Ability };
	public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
	public override States State => States.Stun;
	public override StateType Type => StateType.Physical;
	public override List<StatusEffect> Effects => _effects;


    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
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

		_baseDuration = durationToExit;
	}

	public override void UpdateState()
	{
		_baseDuration -= Time.deltaTime;
		if (_baseDuration < 0)
		{
			GlobalExit();
			return;
		}
		if (turnOff)
		{
			GlobalExit();
		}
	}

	public override bool Stack(float time)
	{
		_baseDuration += time;
		return false;
	}

	protected override void ExitState()
	{
		characterState.Character.Move.IsMoveBlocked = false;
		abilities.SetAbilitiesDisactive(false);
		characterState.RemoveStateFromList(this);
	}
}
