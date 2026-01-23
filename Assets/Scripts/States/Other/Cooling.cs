using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cooling : RefreshingState
{
	public bool turnOff = false;
	private float _damageOnStart;
	private float _damageToExit;
	//private float _curSpeedDebuf = 0.05f;
	private float _speedDebuf = 0.05f;
	private AttributeModifiers _modif = new();

	private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.MoveSpeed, StatusEffect.AbilitySpeed };
	public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
	public override States State => States.Cooling;
	public override StateType Type => StateType.Physical;
	public override List<StatusEffect> Effects => _effects;


    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		_modif.Value = 1 -_speedDebuf;
		_modif.Type = ModifierType.Multiplier;

        //Debug.Log("Entering cooling State");
        characterState = character;
		MaxStacksCount = 5;
		if (damageToExit == 0)
		{
			_damageToExit = 10000;
		}
		else
		{
			_damageToExit = damageToExit;
		}
		_damageOnStart = characterState.Character.Health.SumDamageTaken;

		//characterState.Character.Move.ChangeMoveSpeed(1 - _speedDebuf);
		characterState.Character.Move.AddModifier(_modif);
		currentStacksCount = 1;
		//decrease speed of attact and movement
		//_characterState.Health.sumDamageTaken = 0;
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
		characterState.RemoveState(this);
		//if (!characterState.Check(StatusEffect.MoveSpeed))
		//{
            characterState.Character.Move.RemoveModifier(_modif);
       // }
		if (!characterState.Check(StatusEffect.AbilitySpeed))
		{
			//return speed of attact
		}
	}

    public override bool Stack(float time)
    {
        duration = time;
		if(currentStacksCount < MaxStacksCount)
		{
            characterState.Character.Move.RemoveModifier(_modif);
            currentStacksCount++;
            //characterState.Character.Move.ChangeMoveSpeed(1 - currentStacksCount * _speedDebuf);
			_modif.Value = 1 - currentStacksCount * _speedDebuf;
            characterState.Character.Move.AddModifier(_modif);
        }
        return true;
    }
}
