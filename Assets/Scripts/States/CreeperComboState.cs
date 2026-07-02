using System.Collections.Generic;
using UnityEngine;

public class CreeperComboState : RefreshingState
{
    public override States State => States.CreeperCombo;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;

    public override List<StatusEffect> Effects => new List<StatusEffect>();

    public CreeperComboState()
    {
        MaxStacksCount = 3;
        currentStacksCount = 0;
    }

    public override void EnterState(
        CharacterState character,
        float durationToExit,
        float damageToExit,
        Character personWhoMadeBuff,
        string skillName)
    {
        characterState = character;
        base.personWhoMadeBuff = personWhoMadeBuff;
        duration = durationToExit;
    }

    public override void UpdateState()
    {
    }

    public override bool Stack(float time)
    {
        duration = time;
        return true;
    }

    public void ResetStacks()
    {
        currentStacksCount = 0;
        duration = -1f;
    }

    public override void ExitState()
    {
        ResetStacks();

        if (characterState != null)
            characterState.RemoveState(this);
    }
}