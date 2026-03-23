using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class RestorationStackingState : RefreshingState
{
    private const int _baseMaxStacks = 2;

    private float _baseDuration;
    private float _tickInterval = 3f;
    private float _healPerTick   = 6f;
    private float _timer;
    private bool  _isActive = false;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Restoration };

    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States     State      => States.RestorationStacking;
    public override StateType  Type       => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit,
        Character personWhoMadeBuff, string skillName)
    {
        characterState    = character;
        _baseDuration     = durationToExit;
        duration          = durationToExit;
        MaxStacksCount    = _baseMaxStacks;
        currentStacksCount = 1;
        _timer            = _tickInterval;
        _isActive         = true;

        float healValue = _healPerTick * currentStacksCount + GetSpiritEnergyBonus(characterState.Character);
        CmdHeal(healValue);
    }

    public override void UpdateState()
    {
        if (!_isActive) return;

        _timer -= Time.deltaTime;

        if (_timer <= 0f)
        {
            float healValue = _healPerTick * currentStacksCount + GetSpiritEnergyBonus(characterState.Character);
            CmdHeal(healValue);
            _timer = _tickInterval;
        }
    }

    public override bool Stack(float time)
    {
        if (currentStacksCount < MaxStacksCount)
        {
            currentStacksCount++;
        }

        duration      = _baseDuration;
        RemainingDuration = _baseDuration;

        return true;
    }

    public override void ExitState()
    {
        _isActive          = false;
        duration           = 0f;
        _timer             = 0f;
        currentStacksCount = 0;
        characterState?.RemoveState(this);
        characterState = null;
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
            Value            = healValue,
            DamageableSkill  = null
        };
        health.Heal(ref heal, nameof(RestorationStackingState), null);
    }
}
