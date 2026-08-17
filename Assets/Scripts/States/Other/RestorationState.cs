using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

public class RestorationState : RefreshingState
{
    private const float _tickInterval = 3f;
    private const float _healPerTickBase = 6f;

    private Character _targetCharacter;
    
    private float _baseDuration;
    private float _timer;
    private bool _isActive;
    private bool _healBoostActive;
    private float _bonusHeal;

    private List<StatusEffect> _effects = new List<StatusEffect> { StatusEffect.Restoration };

    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State { get; }
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    public RestorationState(States stateType)
    {
        State = stateType;
    }

    public RestorationState()
    {
    }

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit,
        Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        _baseDuration = durationToExit;
        duration = durationToExit;
        _timer = _tickInterval;
        _isActive = true;
        base.personWhoMadeBuff = personWhoMadeBuff;

        MaxStacksCount = IsStackingMode ? 2 : 1;
        currentStacksCount = 1;

        var restoration = personWhoMadeBuff.Abilities.GetSkill<Restoration>();
        restoration.RestorationHealBooster.Reset();

        _targetCharacter = character.Character;
        _targetCharacter.Health.HealTakedServer += OnTargetHealTaken;
        
        ApplyHealTick();
    }

    public override void UpdateState()
    {
        if (!_isActive) return;

        _timer -= Time.deltaTime;

        if (_timer <= 0f)
        {
            ApplyHealTick();
            _timer = _tickInterval;
        }
    }

    private void ApplyHealTick()
    {
        float baseHeal = _healPerTickBase * currentStacksCount;
        float healValue = baseHeal + GetSpiritEnergyBonus(characterState.Character);
        
        var spark = personWhoMadeBuff?.Abilities?.GetSkill<SparkOfLight>();
        spark?.OverhealManaBooster.OnAnyHealTaken(characterState.Character, healValue, spark);

        var restoration = personWhoMadeBuff?.Abilities?.GetSkill<Restoration>();
        restoration?.RestorationManaBooster.OnRestorationTick(healValue, characterState.Character);
        float bonus = restoration.RestorationHealBooster.BonusHeal;
        healValue += bonus;

        CmdHeal(healValue);
    }

    private float GetSpiritEnergyBonus(Character character)
    {
        var state = character?.CharacterState?.GetState(States.SpiritEnergy) as SpiritEnergyState;
        return state != null ? state.GetHealBonus() : 0f;
    }

    public override bool Stack(float time)
    {
        if (!IsStackingMode)
            return base.Stack(time);
        
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
        
        if (_targetCharacter != null)
        {
            _targetCharacter.Health.HealTakedServer -= OnTargetHealTaken;
            _targetCharacter = null;
        }
        
        characterState?.RemoveState(this);
        characterState = null;
    }

    private bool IsStackingMode => State == States.RestorationStacking;

    [Server]
    private void CmdHeal(float healValue)
    {
        ClientRpcHeal(healValue);
    }

    [ClientRpc]
    private void ClientRpcHeal(float healValue)
    {
        Heal heal = new()
        {
            Value = healValue,
            DamageableSkill = null
        };

        health.Heal(ref heal, nameof(RestorationState), null);
    }
    
    private void OnTargetHealTaken(float healValue, Skill sourceSkill, string sourceName)
    {
        if (sourceSkill == null || healValue <= 0f) return;
        if (sourceSkill.Hero != personWhoMadeBuff) return;

        var restorationSkill = personWhoMadeBuff?.Abilities?.GetSkill<Restoration>();
        if (restorationSkill == null) return;
        
        restorationSkill.RestorationHealBooster.OnHealReceived(healValue);
    }

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
