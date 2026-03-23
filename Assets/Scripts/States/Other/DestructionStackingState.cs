using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class DestructionStackingState : RefreshingState
{
    private const int _baseMaxStacks = 2;

    private float _baseDuration;
    private float _tickInterval  = 4f;
    private float _damagePerTick = 6f;
    private float _timer;
    private bool  _isActive = false;

    private readonly List<StatusEffect> _effects = new() { StatusEffect.Destruction };

    public override States     State      => States.DestructionStacking;
    public override StateType  Type       => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit,
        Character personWhoMadeBuff, string skillName)
    {
        characterState         = character;
        base.personWhoMadeBuff = personWhoMadeBuff;
        _baseDuration          = durationToExit;
        duration               = durationToExit;
        MaxStacksCount         = _baseMaxStacks;
        currentStacksCount     = 1;
        _timer                 = _tickInterval;
        _isActive              = true;

        CmdDamage(_damagePerTick * currentStacksCount);
    }

    public override void UpdateState()
    {
        if (!_isActive) return;

        _timer -= Time.deltaTime;

        if (_timer <= 0f)
        {
            CmdDamage(_damagePerTick * currentStacksCount);
            _timer = _tickInterval;
        }
    }

    public override bool Stack(float time)
    {
        if (currentStacksCount < MaxStacksCount)
            currentStacksCount++;

        duration          = _baseDuration;
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

    [Server] private void CmdDamage(float damageValue) => ClientRpcDamage(damageValue);

    [ClientRpc]
    private void ClientRpcDamage(float damageValue)
    {
        if (!characterState.isServer) return;

        Damage damage = new()
        {
            Value = damageValue,
            Type  = DamageType.Magical,
        };
        health.TryTakeDamage(ref damage, null);
    }
}
