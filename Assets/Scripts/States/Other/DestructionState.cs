using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class DestructionState : RefreshingState
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

    public DestructionState(States stateType)
    {
        State = stateType;
    }

    public DestructionState() { }

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit,
        Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        base.personWhoMadeBuff = personWhoMadeBuff;
        this.damageToExit = damageToExit;

        _baseDuration = durationToExit;
        duration = durationToExit;
        _timer = _tickInterval;
        _isActive = true;

        MaxStacksCount = IsStackingMode ? 2 : 1;
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
        };

        health.TryTakeDamage(ref damage, null);
        
        if (damageToExit == -1f)
        {
            float chance = Random.Range(0f, 100f);
            if (chance <= 15f)
            {
                characterState.AddState(States.SpiritHealth, 18f, 0, characterState.gameObject, nameof(SpiritHealthState));
            }
        }
    }

    public override bool Stack(float time)
    {
        if (!IsStackingMode)
        {
            duration = _baseDuration;
            RemainingDuration = _baseDuration;
            return true;
        }
        
        if (currentStacksCount < MaxStacksCount)
            currentStacksCount++;

        duration = _baseDuration;
        RemainingDuration = _baseDuration;

        return true;
    }

    public override void ExitState()
    {
        _isActive = false;
        duration = 0f;
        _timer = 0f;
        currentStacksCount = 0;
        characterState?.RemoveState(this);
        characterState = null;
    }

    private bool IsStackingMode => State == States.DestructionStacking;
    
    public override AbstractCharacterState TryApply(CharacterState character, float durationToExit, float damageToExit,
        Character personWhoMadeBuff, string skillName)
    {
        if (!CanEnterState(character)) return null;

        BaseInit(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);

        if (currentStacksCount == 0)
        {
            EnterState(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
            currentStacksCount = 1;
        }
        else
        {
            Stack(durationToExit);
        }

        return this;
    }
}
