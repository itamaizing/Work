using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cooling : RefreshingState
{
	public bool turnOff = false;
	private float _damageOnStart;
	private float _damageToExit;
	private float _speedDebuf = -0.05f;
	private AttributeModifier _modif = new AttributeModifier(0f, ModifierType.Percent);

	private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.MoveSpeed, StatusEffect.AbilitySpeed };
	public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
	public override States State => States.Cooling;
	public override StateType Type => StateType.Physical;
	public override List<StatusEffect> Effects => _effects;


    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		_modif = new AttributeModifier(_speedDebuf, ModifierType.Percent);
    
		characterState = character;
		MaxStacksCount = 6;
		_damageToExit = damageToExit == 0 ? 10000 : damageToExit;
		_damageOnStart = characterState.Character.Health.SumDamageTaken;
    
		characterState.Character.Move.AddModifier(_modif);
		currentStacksCount = 1;
	}

	public override void UpdateState()
	{
		if (characterState.Character.Health.SumDamageTaken - _damageOnStart >= _damageToExit || turnOff)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		characterState.Character.Move.RemoveModifier(_modif);
		currentStacksCount = 0;
		turnOff = false;
		_damageOnStart = 0;
		_damageToExit = 0;
		_modif = new AttributeModifier(_speedDebuf, ModifierType.Percent);
		characterState.RemoveState(this);
	}

    public override bool Stack(float time)
    {
        duration = time;
		if(currentStacksCount < MaxStacksCount)
		{
            characterState.Character.Move.RemoveModifier(_modif);
            currentStacksCount++;
			_modif.Value = currentStacksCount * _speedDebuf;
			characterState.Character.Move.AddModifier(_modif);
		}
        return true;
    }
}
