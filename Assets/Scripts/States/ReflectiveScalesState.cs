using System.Collections.Generic;
using UnityEngine;

public class ReflectiveScalesState : StackableState
{
    private float _durationRemaining;

    private List<StatusEffect> _effects = new()
    {
        StatusEffect.Strengthening
    };

    public override States State => States.ReflectiveScales;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;
    public override float RemainingDuration => _durationRemaining;

    public override void EnterState(CharacterState character,
        float durationToExit,
        float damageToExit,
        Character personWhoMadeBuff,
        string skillName)
    {
        characterState = character;
        health = character.Character.Health;
        this.personWhoMadeBuff = personWhoMadeBuff;

        _durationRemaining = durationToExit;

        if (health != null)
        {
            health.OnTryResist += TryReflect;
        }
    }

    public override void UpdateState()
    {
    }

    public override bool Stack(float time)
    {
        _durationRemaining = time;
        return true;
    }

    private bool TryReflect(Damage damage)
    {
        ExitState();

        return true;
    }

    public override void ExitState()
    {
        if (health != null)
        {
            health.OnTryResist -= TryReflect;
        }

        characterState.StateIcons.RemoveItemByState(State);
        characterState.RemoveState(this);
    }
}