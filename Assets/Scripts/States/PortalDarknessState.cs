using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalDarknessState : RefreshingState
{
    public override States State => States.FrostEnergy;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;

    public override List<StatusEffect> Effects => new List<StatusEffect>
    {
        StatusEffect.Freezing
    };

    public override Schools Schools => Schools.Water;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
    }

    public override void UpdateState()
    {

    }

    public override void ExitState()
    {
        base.ExitState();
    }

}