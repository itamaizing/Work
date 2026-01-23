using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LastBreath : AbstractCharacterState
{
	private Character _character;
	private float _durationToExit = 0;
	private AttributeModifiers _modif;

	public override States State => States.LastBreath;
	public override StateType Type => StateType.Magic;
	public override List<StatusEffect> Effects => throw new System.NotImplementedException();
	public override BaffDebaff BaffDebaff => BaffDebaff.Baff;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		_modif.Value = 1.2f;
		_modif.Type = ModifierType.Multiplier;
        _character = character.Character;
		abilities = _character.Abilities;
		_durationToExit = durationToExit;
		health = _character.Health;

		//_character.Move.ChangeMoveSpeed(1.2f);
		_character.Move.AddModifier(_modif);
		for (int i = 0; i < abilities.Abilities.Count; i++)
		{
			abilities.Abilities[i].Buff.AttackSpeed.IncreasePercentage(1.4f);
		}
		health.RegenerationValue *= 4;
		//increase -regen
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
        //decrease -regen
        //_character.Move.ChangeMoveSpeedBack(1.2f);
        _character.Move.RemoveModifier(_modif);
        for (int i = 0; i < abilities.Abilities.Count; i++)
		{
			abilities.Abilities[i].Buff.AttackSpeed.ReductionPercentage(1.4f);
		}
		health.RegenerationValue /= 4;
	}

	public override bool Stack(float time)
	{
		return true;
	}
}
