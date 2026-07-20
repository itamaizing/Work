using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class FireFlash : StackableState
{
    private readonly List<StatusEffect> _effects = new() { StatusEffect.Ability };

    public override States State => States.FireFlash;
    public override StateType Type => StateType.Immaterial;
    public override BaffDebaff BaffDebaff => BaffDebaff.Null;
    public override List<StatusEffect> Effects => _effects;

    private int _сhance => CurrentStacksCount * 10;

    private float _timer;
    private float _remaining;
    private bool _infinite;

    public override float RemainingDuration => _infinite ? 9999 : _remaining;
    public int Chance { get => _сhance; }

    public FireFlash() => MaxStacksCount = 3;

    public void SwitchToFinite()
    {
        _timer = 0f;
        _infinite = false;
        _remaining = Mathf.Clamp(currentStacksCount, 1, 9999);
    }

    public void SwitchToInfinite()
    {
        _infinite = true;
        _timer = 0f;
        duration = 9999;
    }

    protected override void OnEnterState(CharacterState character, float durationToExit, float damageToExit, Character caster, string skillName)
    {
        characterState = character;
        duration = durationToExit;
        _timer = 0f;
        currentStacksCount = 1;
    }

    public override void OnUpdateState()
    {
        if (_infinite) return;

        _timer += Time.deltaTime;

        if (_timer >= 1f)
        {
            _timer = 0f;

            if (currentStacksCount > 0)
            {
                currentStacksCount--;
                characterState.StateIcons.RemoveIconCount();
            }

            _remaining--;
            if (currentStacksCount <= 0) ExitState();
        }
    }

    public override bool Stack(float time)
    {
        if (currentStacksCount >= MaxStacksCount) return false;
        currentStacksCount++;
        if (!_infinite) SwitchToInfinite();
        characterState?.StateIcons?.ActivateIco(State, RemainingDuration, 1, true, MaxStacksCount);
        return true;
    }
}
