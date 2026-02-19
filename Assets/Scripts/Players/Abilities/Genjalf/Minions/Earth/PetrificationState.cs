using System.Collections.Generic;
using UnityEngine;

public class PetrificationState : AbstractCharacterState
{
	private float _duration;
	private float _curSpeedDebuf = 0f;
	public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
	public override States State => States.PetrificationDebuff;
	public override StateType Type => StateType.Magic;
	public override List<StatusEffect> Effects { get; }

	private float _baseMagicResist;
	private float _basePhysicsResist;
	
	public override void EnterState(CharacterState character, float durationToExit, float damageToExit,
		Character personWhoMadeBuff, string skillName)
	{
		_characterState = character;
		CanStack = true;
		_duration = durationToExit;

		_baseMagicResist = _characterState.Character.Health.ResistMagDamage;
		_basePhysicsResist = _characterState.Character.Health.DefPhysDamage;
		
		_characterState.Character.Health.SetMagicDef(80);
		_characterState.Character.Health.SetPhysicDef(80);
		_characterState.Character.Move.CanMove = false;

		foreach (var ability in _characterState.Character.Abilities.Abilities)
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
		_characterState.RemoveState(this);
		foreach (var ability in _characterState.Character.Abilities.Abilities)
		{
			ability.Disactive = false;
		}

		_characterState.Character.Move.CanMove = true;
		
		_characterState.Character.Health.SetMagicDef(_baseMagicResist);
		_characterState.Character.Health.SetPhysicDef(_basePhysicsResist);
	}

	public override bool Stack(float time)
	{
		return true;
	}
}
