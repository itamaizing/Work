using System.Collections.Generic;
using UnityEngine;

public class TransformationDebuff : StackableState
{
	private float _damageOnStart;
	private float _damageToExit;

	private AttributeModifier _modifier = new(-0.8f,ModifierType.Percent);
	public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
	public override States State => States.TransformationDebuff;
	public override StateType Type => StateType.Magic;
	public override List<StatusEffect> Effects { get; }


    protected override void OnEnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		characterState = character;
		//CanStack = true;
		_damageToExit = 1;

		characterState.Character.Move.AddModifier(_modifier);

		foreach (var ability in characterState.Character.Abilities.Abilities)
		{
			ability.Disactive = true;
		}
	}

    public override void OnUpdateState()
	{
		if (characterState.Character.Health.SumDamageTaken - _damageOnStart >= _damageToExit)
		{
			ExitState();
		}
	}

	protected override void OnExitState()
	{
		
		characterState.Character.TransformationComponent.ReturnToInitial();
		foreach (var ability in characterState.Character.Abilities.Abilities)
		{
			ability.Disactive = false;
		}
		if (!characterState.Check(StatusEffect.MoveSpeed))
		{
			characterState.Character.Move.RemoveModifier(_modifier);
		}
	}

	public override bool Stack(float time)
	{
		return true;
	}
}
