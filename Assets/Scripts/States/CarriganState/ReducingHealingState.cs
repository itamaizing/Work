using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ReducingHealingState : AbstractCharacterState
{
    private AbstractCharacterState _state;

    private float _baseReductionHealingValues = 0.8f;
    private float _duration;
    private float _baseDuration;

    private float _startDelayBeforeChecking = 0.5f;
    private float _delayBeforeChecking;

    private Dictionary<AbstractCharacterState, float> _newHealingStatesValues = new();
    private Dictionary<AbstractCharacterState, float> _oldHealingStatesValues = new();

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.ReducingEfficiency };

    public override float TEST_ChangeableValue { get => _baseReductionHealingValues; set => _baseReductionHealingValues = value; }
    public override States State => States.ReducingHealing;
    public override StateType Type => StateType.Physical;
    public override BuffDebuff BuffDebuff => BuffDebuff.Debuff;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        Debug.Log("ReducingHealingState / EnterState");
        Debug.Log("ReducingHealingState / EnterState / ReductionValue = " + TEST_ChangeableValue);
        _characterState = character;

        _duration = durationToExit;
        _baseDuration = durationToExit;

        _delayBeforeChecking = _startDelayBeforeChecking;
    }

    public override void UpdateState()
    {
        _delayBeforeChecking -= Time.deltaTime;
        if (_delayBeforeChecking <= 0)
        {
            if (TEST_ChangeableValue > 0)
            {
                UdpatingDictionaries();

                ReductionHealing();
            }
            _delayBeforeChecking = _startDelayBeforeChecking;
        }

        _duration -= Time.deltaTime;
        if (_duration <= 0)
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        _newHealingStatesValues.Clear();
        _oldHealingStatesValues.Clear();
        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        return false;
    }

    private void UdpatingDictionaries()
    {
        _newHealingStatesValues.Clear();

        List<AbstractCharacterState> healingStates = _characterState.TEST_GetStatesOnEffectAndType(StatusEffect.Healing, StateType.Magic);

        if (healingStates != null)
        {
            foreach (var healingState in healingStates)
            {
                _newHealingStatesValues[healingState] = healingState.TEST_ChangeableValue;

                if (!_oldHealingStatesValues.ContainsKey(healingState))
                {
                    _oldHealingStatesValues[healingState] = healingState.TEST_ChangeableValue;
                }
            }


            var statesToRemove = new List<AbstractCharacterState>();
            foreach (var state in _oldHealingStatesValues.Keys)
            {
                if (!healingStates.Contains(state))
                {
                    statesToRemove.Add(state);
                }
            }
            foreach (var state in statesToRemove)
            {
                _oldHealingStatesValues.Remove(state);
                _newHealingStatesValues.Remove(state);
            }
        }
    }

    private void ReductionHealing()
    {
        List<AbstractCharacterState> healingStates = new();
        Debug.Log("ReducingHealingState / ReductionHealing");

        _characterState.TEST_GetStatesOnEffectAndType(StatusEffect.Healing, StateType.Magic);

        foreach (var stateEntry in _newHealingStatesValues)
        {
            AbstractCharacterState state = stateEntry.Key;
            Debug.Log("ReducingHealingState / ReductionHealing / state = " + state);
            float newHealingValue = stateEntry.Value;
            float oldHealingValue = _oldHealingStatesValues[state];

            // ≈сли новое значение отличаетс€ от старого, пересчитываем
            if (newHealingValue != oldHealingValue)
            {
                // ѕересчет значени€ с учетом снижени€
                float reductionHealingValue = newHealingValue * _baseReductionHealingValues;
                state.TEST_ChangeableValue = newHealingValue - reductionHealingValue;
                Debug.Log($"State.CurrentValue = " + state.TEST_ChangeableValue);

                // ќбновл€ем старое значение в словаре
                _oldHealingStatesValues[state] = state.TEST_ChangeableValue;

                Debug.Log($"ReducingHealingState / ReductionHealing / newValue: {state.TEST_ChangeableValue}, oldValue: {oldHealingValue}");
            }
        }
    }
}
