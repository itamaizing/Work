using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class Plague : RefreshingState
{
    private int _stack = 1;
    private float _tickTimer = 3f;

    private const int MaxStacks = 3;
    private const float TickInterval = 3f;
    private const float DurationTime = 12f;

    public override States State => States.Plague;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;

    public override List<StatusEffect> Effects => throw new System.NotImplementedException();

    public override void EnterState(CharacterState character,
        float durationToExit,
        float damageToExit,
        Character personWhoMadeBuff,
        string skillName)
    {
        characterState = character;
        duration = DurationTime;
        _tickTimer = TickInterval;
    }

    public override void UpdateState()
    {
        if (!NetworkServer.active) return;

        _tickTimer -= Time.deltaTime;

        if (_tickTimer > 0) return;

        _tickTimer = TickInterval;

        float maxHp = characterState.Character.Health.MaxValue;

        float damageValue = maxHp * 0.01f * _stack;

        characterState.Character.Health.TryUse(damageValue);
    }

    public override bool Stack(float time)
    {
        if (_stack < MaxStacks) _stack++;


        return true;
    }

    public override void ExitState()
    {
        base.ExitState();
    }
}