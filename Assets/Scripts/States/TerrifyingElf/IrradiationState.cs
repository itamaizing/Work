using System.Collections.Generic;
using UnityEngine;

public class IrradiationState : AbstractCharacterState
{
    private float _baseDuration;
    private const float _magicDefenseReduction = 0.03f;
    private const float _durationIncrease = 1.0f;
    private float _duration;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Ability};
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override States State => States.Irradiation;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        Debug.Log("Entering Irradiation State");
        _characterState = character;
        _personWhoMadeBuff = personWhoMadeBuff;
        _baseDuration = durationToExit;
        _duration = _baseDuration;

        MaxStacksCount = 3;

        ApplyMagicDefenseReduction();
        ExtendNegativeMagicEffectsDuration();
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;
        if (_duration <= 0)
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        Debug.Log("Exiting Irradiation State");
        _characterState.RemoveState(this);

        RestoreMagicDefense();
    }

    public override bool Stack(float time)
    {
        if (CurrentStacksCount < MaxStacksCount)
        {
            CurrentStacksCount++;
            _duration = _baseDuration;
            ApplyMagicDefenseReduction();
            ExtendNegativeMagicEffectsDuration();

            Debug.Log($"Stacking Irradiation. Current stacks: {CurrentStacksCount}, New duration: {_duration}s");
            return true;
        }
        else
        {
            _duration = _baseDuration;
            Debug.Log($"Max stacks reached. Refreshing Irradiation duration: {_duration}s");
            return false;
        }
    }

    private void ApplyMagicDefenseReduction()
    {
        _characterState.Character.Health.DefMagDamage -= _magicDefenseReduction;
    }

    private void RestoreMagicDefense()
    {
        _characterState.Character.Health.DefMagDamage += _magicDefenseReduction * CurrentStacksCount;
    }

    private void ExtendNegativeMagicEffectsDuration()
    {
        foreach (var state in _characterState.CurrentStates)
        {
            if (state != this && state.Type == StateType.Magic)
            {
                state.Stack(_baseDuration + _durationIncrease);
            }
        }
    }
}
