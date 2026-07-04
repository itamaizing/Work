using System.Collections.Generic;
using UnityEngine;

public class HealingSlime : RefreshingState
{
    public override States State => States.HealingSlime;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    private readonly List<StatusEffect> _effects = new() { StatusEffect.Healing };

    private const float PercentPerStack = 0.01f;

    private float _timer;
    private float _remaining;
    private bool _infinite;

    public override float RemainingDuration => _infinite ? 999f : _remaining;

    public HealingSlime()
    {
        MaxStacksCount = 9;
    }

    public void SwitchToFinite()
    {
        _timer = 0f;
        _infinite = false;
        _remaining = Mathf.Clamp(currentStacksCount, 1, 999f);
    }

    public void SwitchToInfinite()
    {
        _infinite = true;
        _timer = 0f;
        duration = 999f;
    }

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character caster, string skillName)
    {
        health = character.Character.Health;

        currentStacksCount = 0;
        SwitchToInfinite();
        Stack(0);
    }

    public override void UpdateState()
    {
        if (_infinite) return;

        _timer += Time.deltaTime;
        if (_timer >= 1f)
        {
            _timer = 0f;

            if (currentStacksCount > 0)
            {
                currentStacksCount--;
                float removeValue = Mathf.Floor(health.MaxValue * PercentPerStack);
                health.AddMax(-removeValue);
                characterState.StateIcons.RemoveIconCount();
            }

            _remaining -= 1f;
            if (_remaining <= 0f || currentStacksCount <= 0) ExitState();
        }
    }

    public override bool Stack(float _)
    {
        if (currentStacksCount < MaxStacksCount) currentStacksCount++;
        float addValue = Mathf.Floor(health.MaxValue * PercentPerStack);
        health.AddMax(addValue);

        if (!_infinite) SwitchToInfinite();
        return true;
    }

    public override void ExitState()
    {
        if (currentStacksCount > 0)
        {
            float removeValue = Mathf.Floor(health.MaxValue * PercentPerStack * currentStacksCount);
            health.AddMax(-removeValue);
        }

        characterState.RemoveState(this);
    }
}
