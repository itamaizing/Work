using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComboState : RefreshingState
{
    private float _durationRemaining;
    private string _skillName;
    public int InitialStackCount = 3;

    public override States State => States.ComboState;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;

    public override List<StatusEffect> Effects => new List<StatusEffect>() { StatusEffect.Strengthening };

    public ComboState()
    {
        MaxStacksCount = 3;
        currentStacksCount = 0;
    }

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        base.personWhoMadeBuff = personWhoMadeBuff;
        _durationRemaining = durationToExit;
        _skillName = skillName;
        if (skillName == "ComboIncreaseStacks")
        {
            MaxStacksCount += 1;
        }
        currentStacksCount = 1;
    }

    public override void UpdateState()
    {
        if (_durationRemaining <= 0f)
        {
            //ExitState();
            return;
        }
        //_durationRemaining -= Time.deltaTime;
    }

    public override void ExitState()
    {
        currentStacksCount = 0;
        characterState.RemoveState(this);
    }
    
    public override void ReduceStack()
    {
        currentStacksCount--;

        if (currentStacksCount <= 0)
        {
            characterState.StateIcons.RemoveItemByState(State);
            ExitState();
        }
        else
        {
            characterState.StateIcons.ActivateIco(State, float.PositiveInfinity, -1, true, MaxStacksCount);
        }
    }

    public override bool Stack(float time)
    {
        if (currentStacksCount < MaxStacksCount)
        {
            currentStacksCount++;
            return true;
        }

        return true;
    }
    
    public override AbstractCharacterState TryApply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        if (!CanEnterState(character)) return null;

        BaseInit(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);

        if (currentStacksCount == 0)
            EnterState(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
        else
            Stack(duration);

        return this;
    }
}
