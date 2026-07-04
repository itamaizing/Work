using System.Collections.Generic;
using UnityEngine;

public class PetrificationState : StackableState
{
	private float _duration;
	private float _curSpeedDebuf = 0f;
	public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
	public override States State => States.PetrificationDebuff;
	public override StateType Type => StateType.Magic;
	public override List<StatusEffect> Effects { get; }

	private float _baseMagicResist;
	private float _basePhysicsResist;

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit,
		Character personWhoMadeBuff, string skillName)
	{
		characterState = character;
		//CanStack = true;
		_duration = durationToExit;

		_baseMagicResist = characterState.Character.Health.ResistMagDamage;
		_basePhysicsResist = characterState.Character.Health.DefPhysDamage;
		
		characterState.Character.Health.SetMagicDef(80);
		characterState.Character.Health.SetPhysicDef(80);
		characterState.Character.Move.SetCanMove(false);

		foreach (var ability in characterState.Character.Abilities.Abilities)
		{
			ability.Disactive = true;
		}
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
		foreach (var ability in characterState.Character.Abilities.Abilities)
		{
			ability.Disactive = false;
		}

		characterState.Character.Move.SetCanMove(true);
		
		characterState.Character.Health.SetMagicDef(_baseMagicResist);
		characterState.Character.Health.SetPhysicDef(_basePhysicsResist);
	}

	public override bool Stack(float time)
	{
		return true;
	}
}
