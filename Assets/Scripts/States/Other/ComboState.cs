using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComboState : StackableState
{
    public override States State => States.ComboState;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;

    public override List<StatusEffect> Effects => new List<StatusEffect>() { StatusEffect.Strengthening };

    public ComboState()
    {
        MaxStacksCount = 3;
        currentStacksCount = 1;
    }

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        base.personWhoMadeBuff = personWhoMadeBuff;
        //this.s = skillName;
    }

    public override void UpdateState()
    {
    }

    public override bool Stack(float time)
    {
        if (currentStacksCount < MaxStacksCount)
        {
            currentStacksCount++;
            duration = time;

            return false;
        }

        return false;
    }
}
