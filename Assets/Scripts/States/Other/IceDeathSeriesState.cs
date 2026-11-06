using System.Collections.Generic;
using UnityEngine;

public sealed class IceDeathSeriesState : StateBasic
{
    private readonly States _state;
    private bool _paused;

    public IceDeathSeriesState(States state) => _state = state;

    public override States State => _state;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => new();

    public override int CurrentStacksCount { get; protected set; }
    public override int MaxStacksCount => int.MaxValue;

    public override string TooltipName => "Серия приёмов";

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character sourceCaster, string skillName)
    {
    }

    public void SetSnapshot(int hitCount, float remaining, float maximum, bool paused)
    {
        CurrentStacksCount = hitCount;
        DisplayText = hitCount.ToString();

        MaxDuration = maximum;
        RemainingDuration = Mathf.Clamp(remaining, 0f, maximum);
        _paused = paused;
    }

    public override void GlobalUpdate()
    {
        if (!_paused)
            RemainingDuration = Mathf.Max(0f, RemainingDuration - Time.deltaTime);
    }
}