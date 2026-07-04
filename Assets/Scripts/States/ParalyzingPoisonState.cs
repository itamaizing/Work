using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParalyzingPoisonState : AbstractCharacterState
{
	public bool turnOff = false;
	//private PlayerAbilities _abilities;
	private float _baseDuration;

	private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.Ability, StatusEffect.Poison };
	public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
	public override States State => States.ParalyzingPoison;
	public override StateType Type => StateType.Physical;
	public override List<StatusEffect> Effects => _effects;


    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{

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
		if (turnOff)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		characterState.RemoveState(this);
		if (!characterState.Check(StatusEffect.Move)) characterState.Character.Move.IsMoveBlocked = false;
		if (!characterState.Check(StatusEffect.Ability) && abilities != null) abilities.SetAbilitiesDisactive(false);
	}
}
