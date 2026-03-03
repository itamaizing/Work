using System.Collections.Generic;
using UnityEngine;

public class PushingWindBuff : AbstractCharacterState
{
	private float _duration;
	private float _curSpeedBuf = 0.3f;
	public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
	public override States State => States.PushingWindBuff;
	public override StateType Type => StateType.Magic;
	public override List<StatusEffect> Effects { get; }

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit,
		Character personWhoMadeBuff, string skillName)
	{
		characterState = character;
		_duration = durationToExit;

		//characterState.Character.Move.ChangeMoveSpeed(1 + _curSpeedBuf); TODO: Переделать на систему аттрибутов
	}

	public override void UpdateState()
	{
		_duration -= Time.deltaTime;
		if (_duration < 0)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		characterState.RemoveState(this);
		characterState.Character.Move.SetDefaultSpeed(); //TODO: Переделать на систему аттрибутов

    }

	public override bool Stack(float time)
	{
		return true;
	}
}
