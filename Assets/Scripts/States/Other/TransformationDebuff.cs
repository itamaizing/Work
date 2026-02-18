using System.Collections.Generic;
using UnityEngine;

public class TransformationDebuff : AbstractCharacterState
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
		_characterState = character;
		CanStack = true;
		_damageToExit = 1;
		_duration = durationToExit;
		
		_characterState.Character.Move.SetMoveSpeed(_curSpeedDebuf);

		foreach (var ability in _characterState.Character.Abilities.Abilities)
		{
			ability.Disactive = true;
		}
	}

    public override void UpdateState()
	{
		_duration -= Time.deltaTime;
		if (_characterState.Character.Health.SumDamageTaken - _damageOnStart >= _damageToExit || _duration < 0)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		_characterState.RemoveState(this);
		_characterState.Character.TransformationComponent.ReturnToInitial();
		foreach (var ability in _characterState.Character.Abilities.Abilities)
		{
			ability.Disactive = false;
		}
		if (!_characterState.Check(StatusEffect.MoveSpeed))
		{
			_characterState.Character.Move.SetDefaultSpeed();
		}
	}

	public override bool Stack(float time)
	{
		return true;
	}
}
