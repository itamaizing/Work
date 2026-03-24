using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

public class DestructionState : RefreshingState
{
    private readonly List<StatusEffect> _effects = new() { StatusEffect.Destruction };
    public override States State => States.Destruction;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => _effects;
    
    private float _tickInterval = 4f;
    private float _damagePerTick = 6f;

    private float _duration;
    private float _timer;
    private bool _isActive = false;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit,
        Character personWhoMadeBuff, string skillName)
    {
        _duration = durationToExit;
        characterState = character;
        this.damageToExit = damageToExit;
        base.personWhoMadeBuff = personWhoMadeBuff;
        MaxStacksCount = 1;
        currentStacksCount = 1;
        _timer = _tickInterval;
        _isActive = true;
        CmdDamage(_damagePerTick);
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
            CmdDamage(_damagePerTick);
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

    [Server] private void CmdDamage(float damageValue) => ClientRpcDamage(damageValue);

    [ClientRpc]
    private void ClientRpcDamage(float damageValue)
    {
        Damage damage = new()
        {
            Value = damageValue,
            Type = DamageType.Magical,
        };
        health.TryTakeDamage(ref damage, null);
        CmdAddSpiritHealth();
    }

    [Command]
    private void CmdAddSpiritHealth()
    {
        if (damageToExit == -1f)
        {
            float chance = UnityEngine.Random.Range(0f, 100f);
            if (chance <= 15)
                characterState.AddState(States.SpiritHealth, 18, 0, characterState.gameObject, nameof(SpiritHealthState));
        }
    }
}
