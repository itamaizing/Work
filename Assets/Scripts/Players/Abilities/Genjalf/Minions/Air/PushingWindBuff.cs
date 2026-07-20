using System.Collections.Generic;
using UnityEngine;

public class PushingWindBuff : AbstractCharacterState
{
	private float _duration;

	private float _speedModifier = 0.3f;
	private AttributeModifier _modifier = new(0,ModifierType.Multiplier);
	public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
	public override States State { get; }
	public override StateType Type => StateType.Magic;
	public override List<StatusEffect> Effects { get; }

	private bool isAuraState => State == States.PushingWindAura;

	public PushingWindBuff(States stateType)
	{
		State = stateType;
	}

    protected override void OnEnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		characterState = character;

		_speedModifier = isAuraState ? 0.1f : 0.3f;

		_modifier.Value = 1 + _speedModifier;
		_modifier.Type = ModifierType.Multiplier;
		characterState.Character.Move.AddModifier(_modifier);
	}

	public override void OnUpdateState()
	{
		if(isAuraState) return;
		_duration -= Time.deltaTime;
		if (_duration < 0)
		{
			ExitState();
		}
	}

	protected override void OnExitState()
	{
		characterState.Character.Move.RemoveModifier(_modifier);
		characterState.RemoveStateFromList(this);

    }

	/*public override bool Stack(float time)
	{
		return false;
	}*/
}
