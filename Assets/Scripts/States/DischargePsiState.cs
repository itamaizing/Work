using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DischargePsiState : AbstractCharacterState
{
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State => States.DischargePsi;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Ability };

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
    }

    /*public override bool Stack(float time)
    {
        return false;
    }*/

    public override void UpdateState()
    {
    }
}
