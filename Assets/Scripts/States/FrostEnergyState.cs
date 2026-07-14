using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrostEnergyState : RefreshingState
{
    public override States State => throw new System.Exception("none");
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;

    public override List<StatusEffect> Effects => new List<StatusEffect>
    {
        StatusEffect.Freezing
    };

    public override Schools Schools => Schools.Water;

    protected override void OnEnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
    }

    public override void OnUpdateState()
    {

    }

    public override void ExitState()
    {
        base.ExitState();
    }

}