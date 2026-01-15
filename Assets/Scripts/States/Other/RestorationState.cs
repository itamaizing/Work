using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

public class RestorationState : AbstractCharacterState
{
    public override States State => States.Restoration;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => new() { StatusEffect.Restoration };

    private float _tickInterval = 4f;
    private float _healPerTick = 6f;
    private float _effectivenessIncreasePerHeal = 0.1f;

    private float _timer;
    private float _accumulatedEffectiveness = 1f;
    private float _totalHealedInInterval = 0f;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        health = character.Character.Health;
        _accumulatedEffectiveness = 1f;
        _totalHealedInInterval = 0f;

        _timer = _tickInterval;

		float spiritBonus = GetSpiritEnergyBonus(characterState.Character);
		float healValue = _healPerTick * _accumulatedEffectiveness + spiritBonus;
		CmdHeal(healValue);
	}

    public override void UpdateState()
    {
        if (health == null) return;

        _timer -= Time.deltaTime;

        if (_timer <= 0f)
        {
            float spiritBonus = GetSpiritEnergyBonus(characterState.Character);
            float healValue = _healPerTick * _accumulatedEffectiveness + spiritBonus;

            CmdHeal(healValue);

             _accumulatedEffectiveness += _totalHealedInInterval * _effectivenessIncreasePerHeal;
            _totalHealedInInterval = healValue;

            _timer = _tickInterval;
        }
    }

    private float GetSpiritEnergyBonus(Character character)
    {
        var state = character?.CharacterState?.GetState(States.SpiritEnergy) as SpiritEnergyState;
        return state != null ? state.GetHealBonus() : 0f;
    }

    public override void ExitState()
    {
        characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        duration = time;
        return false;
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
