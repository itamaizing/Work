using System.Collections.Generic;
using UnityEngine;

public class TiredSoul : AbstractCharacterState
{
    private float _baseDuration;

    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State => States.TiredSoul;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => new List<StatusEffect>();

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        _baseDuration = durationToExit;
        currentStacksCount++;
        MaxStacksCount = 2;
    }

    public override void UpdateState()
    { 
        if (duration <= _baseDuration * (currentStacksCount - 1) && currentStacksCount > 0)
        {
            currentStacksCount--;
            duration = _baseDuration * currentStacksCount;

            if (currentStacksCount == 0)
            {
                ExitState();
            }
        }
    }

    public override void ExitState()
    {
       if(!characterState.CheckForState(States.TiredSoul)) 
           return;
       
       characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        if (currentStacksCount < MaxStacksCount)
        {
            currentStacksCount++;
            duration += time;
            duration = Mathf.Min(duration, _baseDuration * currentStacksCount);
        }
        return true;
    }
}