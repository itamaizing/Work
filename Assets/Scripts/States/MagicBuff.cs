using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicBuff : AbstractCharacterState
{
	private Character _character;
	private float _durationToExit;
	private float _shieldCapacity;
    public override float CurrentValue { get; set; }
    public override States State => States.MagicBuff;
	public override StateType Type => StateType.Magic;
	public override List<StatusEffect> Effects => throw new System.NotImplementedException();

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		_character = character.Character;
		_durationToExit = durationToExit;
		_shieldCapacity = damageToExit;

		//_character.Health.SetMagAbsorb(_shieldCapacity);
	}

	public override void UpdateState()
	{
		_durationToExit -= Time.deltaTime;
		if (_durationToExit < 0)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		//_character.Health.SetMagAbsorb(0);
	}

	public override bool Stack(float time)
	{
		_durationToExit = time;
		return true;
	}
}
