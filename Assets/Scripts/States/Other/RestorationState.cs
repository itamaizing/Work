using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

public class RestorationState : RefreshingState
{
    private float _duration;
    private float _tickInterval = 3f;
    private float _healPerTick = 6f;
    private float _timer;
    private bool _isActive = false;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Restoration };

    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State => States.Restoration;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit,
        Character personWhoMadeBuff, string skillName)
    {
        _duration = durationToExit;
        characterState = character;
        MaxStacksCount = 1;
        currentStacksCount = 1;
        _timer = _tickInterval;
        _isActive = true;

        float healValue = _healPerTick + GetSpiritEnergyBonus(characterState.Character);
        CmdHeal(healValue);
    }

    public override void UpdateState()
    {
        if (!_isActive) return;

        _duration -= Time.deltaTime;
        if (_duration < 0)
        {
            ExitState();
            return;
        }

        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            float healValue = _healPerTick + GetSpiritEnergyBonus(characterState.Character);
            CmdHeal(healValue);
            _timer = _tickInterval;
        }
    }

    public override void ExitState()
    {
        _isActive = false;
        _duration = 0f;
        _timer = 0f;
        currentStacksCount = 0;
        characterState?.RemoveState(this);
        characterState = null;
    }

    public override bool Stack(float time)
    {
        _duration += time;
        RemainingDuration = _duration;
        return false;
    }

    private float GetSpiritEnergyBonus(Character character)
    {
        var state = character?.CharacterState?.GetState(States.SpiritEnergy) as SpiritEnergyState;
        return state != null ? state.GetHealBonus() : 0f;
    }

    [Server] private void CmdHeal(float healValue) => ClientRpcHeal(healValue);

    [ClientRpc]
    private void ClientRpcHeal(float healValue)
    {
        Heal heal = new()
        {
            Value = healValue,
            DamageableSkill = null
        };
        health.Heal(ref heal, "RestorationState", null);
    }
}
