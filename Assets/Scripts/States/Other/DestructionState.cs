using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class DestructionStateStacking : RefreshingStateStacking
{
    private const float _tickInterval = 4f;
    private const float _damagePerTickBase = 6f;

    private float _baseDuration;
    private float _timer;
    private bool _isActive;

    private readonly List<StatusEffect> _effects = new() { StatusEffect.Destruction };

    public override States State { get; }
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => _effects;

    public DestructionStateStacking(States stateType)
    {
        State = stateType;
    }

    public DestructionStateStacking() { }

    public override void Apply(CharacterState character, float durationToExit, float damageToExit,
        Character personWhoMadeBuff, string skillName)
    {
        characterState = character;

        _baseDuration = durationToExit;
        RemainingDuration = durationToExit;
        _timer = _tickInterval;
        _isActive = true;

        SetMaxStacks(IsStackingMode ? 2 : 1);
        currentStacksCount = 1;

        ApplyDamageTick();
    }

    public override void UpdateState()
    {
        if (!_isActive) return;

        _timer -= Time.deltaTime;

        if (_timer <= 0f)
        {
            ApplyDamageTick();
            _timer = _tickInterval;
        }
    }

    private void ApplyDamageTick()
    {
        int effectiveStacks = Mathf.Min(currentStacksCount, MaxStacksCount);
        float damageValue = _damagePerTickBase * effectiveStacks;

        CmdDamage(damageValue);
    }

    private void CmdDamage(float damageValue)
    {
        ClientRpcDamage(damageValue);
    }

    [ClientRpc]
    private void ClientRpcDamage(float damageValue)
    {
        Damage damage = new()
        {
            Value = damageValue,
            Type = DamageType.Magical,
            School = Schools.Dark
        };

        if (sourceCaster != null && sourceCaster.isOwned)
            sourceCaster.Abilities.GetSkill<Restoration>().CmdApplyDamage(damage,characterState.gameObject);

        if (DamageToExit == float.MaxValue)
        {
            float chance = Random.Range(0f, 100f);
            if (chance <= 15f)
            {
                characterState.AddState(States.SpiritHealth, 18f, 0, characterState.gameObject, nameof(SpiritHealthStateStacking));
            }
        }
    }

    public override bool Stack(float time)
    {
        if (!IsStackingMode)
        {
            RemainingDuration = _baseDuration;
            RemainingDuration = _baseDuration;
            return true;
        }
        
        if (currentStacksCount < MaxStacksCount)
            currentStacksCount++;

        RemainingDuration = _baseDuration;
        RemainingDuration = _baseDuration;

        return true;
    }

    public override void ExitState()
    {
        _isActive = false;
        RemainingDuration = 0f;
        _timer = 0f;
        currentStacksCount = 0;
        characterState?.RemoveState(this);
        characterState = null;
    }

    private bool IsStackingMode => State == States.DestructionStacking;
    
    
}
