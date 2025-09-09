using System;
using System.Collections.Generic;
using UnityEngine;

public class RestorationState : AbstractCharacterState
{
    private List<StatusEffect> _effects = new() { StatusEffect.Restoration };

    public override States State => States.Restoration;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _characterState = character;
        _health = character.Character.Health;
        _abilities = character.Character.Abilities;
        _personWhoMadeBuff = personWhoMadeBuff;
        duration = durationToExit;
    }

    public override void ExitState()
    {
        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        duration = time;
        return false;
    }

    public override void UpdateState()
    {
        duration -= Time.deltaTime;

        if (duration <= 0)
        {
            ExitState();
            return;
        }
    }
}
