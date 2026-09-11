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

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
    }

    public override void ExitState()
    {
        
        characterState.RemoveState(this);
    }

    public override void UpdateState()
    {
    }
}
