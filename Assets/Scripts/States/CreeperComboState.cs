using System.Collections.Generic;
using UnityEngine;

public class CreeperComboStateStacking : RefreshingStateStacking
{
    public override States State => States.CreeperCombo;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;

    public override List<StatusEffect> Effects => new List<StatusEffect>();

    public CreeperComboStateStacking()
    {
        SetMaxStacks(3);
        currentStacksCount = 0;
    }

    public override void Apply(
        CharacterState character,
        float durationToExit,
        float damageToExit,
        Character personWhoMadeBuff,
        string skillName)
    {
        characterState = character;
        
        RemainingDuration = durationToExit;
    }

    public override void UpdateState()
    {
    }

    public override bool Stack(float time)
    {
        RemainingDuration = time;
        return true;
    }

    public void ResetStacks()
    {
        currentStacksCount = 0;
        RemainingDuration = -1f;
    }

    public override void ExitState()
    {
        ResetStacks();

        if (characterState != null)
            characterState.RemoveState(this);
    }
}