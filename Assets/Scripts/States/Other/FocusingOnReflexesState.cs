using System.Collections.Generic;
using UnityEngine;

public class FocusingOnReflexesStateStacking : StateStackingRefreshing
{
    private readonly List<StatusEffect> _effects = new() { StatusEffect.Evade };

    public override States State => States.FocusingOnReflexesState;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    public FocusingOnReflexesStateStacking()
    {
        SetMaxStacks(1);
        CurrentStacksCount = 0;
    }

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        this.sourceCaster = personWhoMadeBuff;
        CurrentStacksCount = 1;
    }

    public override void UpdateState()
    {
    }

    public override bool Stack(float time)
    {
        RemainingDuration = time;
        return true;
    }

    public override void ExitState()
    {
        CurrentStacksCount = 0;
        
        characterState.RemoveState(this);
    }

    
}