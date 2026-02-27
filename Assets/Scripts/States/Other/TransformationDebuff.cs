using System.Collections.Generic;
using UnityEngine;

public class TransformationDebuff : StackableState
{
	private float _duration;
	private float _damageOnStart;
	private float _damageToExit;
	private float _curSpeedDebuf = 0.6f;
	public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
	public override States State => States.TransformationDebuff;
	public override StateType Type => StateType.Magic;
	public override List<StatusEffect> Effects { get; }


    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		characterState = character;
		//CanStack = true;
		_damageToExit = 1;
		_duration = durationToExit;
		
		//characterState.Character.Move.SetMoveSpeed(_curSpeedDebuf); //TODO: Переписать на атрибут

		foreach (var ability in characterState.Character.Abilities.Abilities)
		{
			ability.Disactive = true;
		}
	}

    public override void UpdateState()
	{
		_duration -= Time.deltaTime;
		if (characterState.Character.Health.SumDamageTaken - _damageOnStart >= _damageToExit || _duration < 0)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		characterState.RemoveState(this);
		characterState.Character.TransformationComponent.ReturnToInitial();
		foreach (var ability in characterState.Character.Abilities.Abilities)
		{
			ability.Disactive = false;
		}
		if (!characterState.Check(StatusEffect.MoveSpeed))
		{
			characterState.Character.Move.SetDefaultSpeed();
		}
	}

	public override bool Stack(float time)
	{
		return true;
	}
}
