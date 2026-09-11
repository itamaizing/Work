using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComboStateStacking : RefreshingStateStacking
{
    private float _durationRemaining;
    private string _skillName;
    public int InitialStackCount = 3;

    public override States State => States.ComboState;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;

    public override List<StatusEffect> Effects => new List<StatusEffect>() { StatusEffect.Strengthening };

    public ComboStateStacking()
    {
        currentStacksCount = 0;
    }

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        SetMaxStacks(3);
        characterState = character;
        
        _durationRemaining = durationToExit;
        _skillName = skillName;
        if (skillName == "ComboIncreaseStacks")
        {
            SetMaxStacks(MaxStacksCount + 1);
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
            
            ExitState();
        }
        else
        {

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
    
    
}
