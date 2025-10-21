using System.Collections.Generic;
using UnityEngine;

public class HealingSlime : AbstractCharacterState
{
    public override States State => States.HealingSlime;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    private readonly List<StatusEffect> _effects = new() { StatusEffect.Healing };

    private const float MaxPercent = 0.09f;
    private const float BonusPerSecond = 0.01f;

    private float _timer;
    private float _accumulatedPercent;
    private bool _isDecreasing;

    public override float RemainingDuration => duration;

    public HealingSlime()
    {
        MaxStacksCount = 1;
    }

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character caster, string skillName)
    {
        _characterState = character;
        _personWhoMadeBuff = caster;
        _health = character.Character.Health;

        _timer = 0f;
        _accumulatedPercent = 0f;
        _isDecreasing = false;

        duration = 999f;
    }

    public override void UpdateState()
    {
        _timer += Time.deltaTime;
        if (_timer < 1f) return;
        _timer = 0f;

        if (!_isDecreasing)
        {
            if (_accumulatedPercent < MaxPercent)
            {
                float addPercent = Mathf.Min(BonusPerSecond, MaxPercent - _accumulatedPercent);
                float addValue = _health.MaxValue * addPercent;

                _health.AddMax(addValue);
                _accumulatedPercent += addPercent;
            }
        }
        else
        {
            if (_accumulatedPercent > 0f)
            {
                float removePercent = Mathf.Min(BonusPerSecond, _accumulatedPercent);
                float removeValue = _health.MaxValue * removePercent;

                _health.AddMax(-removeValue);
                _accumulatedPercent -= removePercent;

                if (_health.CurrentValue > _health.MaxValue) _health.CurrentValue = _health.MaxValue;
            }
            else
            {
                _characterState.RemoveState(this);
            }
        }
    }

    public override bool Stack(float time)
    {
        duration = 999f;
        ResetDecreasePhase();
        return true;
    }

    public override void ExitState()
    {
        BeginDecreasePhase();
    }

    public void BeginDecreasePhase()
    {
        _isDecreasing = true;
        _timer = 0f;
    }

    public void ResetDecreasePhase()
    {
        _isDecreasing = false;
        _timer = 0f;
    }
}
