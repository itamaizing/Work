using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class SparkOfLight : AutoAttackSkill
{
    [Header("Spark Of Light Settings")]
    [SerializeField] private float _buffDuration = 9f;
    [SerializeField] private float _healAmount = 2f;
    [SerializeField] private float _damageAmount = 20f;
    [SerializeField] private float _castTime = 0.8f;
    [SerializeField] private float _range = 4f;
    [SerializeField] private List<SkillEnergyCost> _manaCostHeal;
    [SerializeField] private List<SkillEnergyCost> _manaCostDamage;

    [Header("Alternative Mode Settings")]
    [SerializeField] private float _altRange = 6f;
    [SerializeField] private float _altBuffDuration = 5f;
    [SerializeField] private float _altDamageAmount = 2f;
    [SerializeField] private List<SkillEnergyCost> _altManaCostDamage;
    [SerializeField] private FlashOfLight _flashOfLight;

    public bool IsLightMode = true;

    private bool _healthBoostActive = false;
    private bool _lowHealthTalentActive = false;
    private bool _manaRestoreBoostTalent = false;
    private bool _healingBuffTalentActive = false;
    
    private const float LowHealthThreshold = 0.25f;
    private const float BonusDamageMultiplier = 1.25f;
    private const float HealthBoostPercentage = 0.25f;
    private const float HealthBoostDuration = 2f;
    private const float DefenseReductionPercentage = 0.25f;
    private const float DefenseDebuffDuration = 2f;
    
    private float _healingBuffDuration = 5f;
    private float _tickHealingBonus = 2f;
    private int _healingBonusStacks = 0;
    private float _lastFlashOfLightCastTime = 0f;

    public void EnableTalentPhysicalShieldBoost(bool value) => _healthBoostActive = value;
    public void EnableLowHealthTalent(bool value) => _lowHealthTalentActive = value;
    public void EnableManaRestoreBoostTalent(bool value) => _manaRestoreBoostTalent = value;
    public void EnableHealingBuffTalent(bool value) => _healingBuffTalentActive = value;
    
    private bool IsAllyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");
    private bool IsEnemyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Enemy");

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerAutoAttack => throw new NotImplementedException();

    public event Action OnModeChange;

    private void OnEnable()
    {
        _flashOfLight.CastEnded += HandleLastTimeFlashOfLightCast;
        OnModeChange += HandleModeChange;
        UpdateMode();
    }

    private void OnDisable()
    {
        _flashOfLight.CastEnded -= HandleLastTimeFlashOfLightCast;
        OnModeChange -= HandleModeChange;
    }

    public void SwitchMode()
    {
        IsLightMode = !IsLightMode;
        OnModeChange?.Invoke();
    }

    private void HandleModeChange()
    {
        UpdateMode();
    }

    private void HandleLastTimeFlashOfLightCast()
    {
        _lastFlashOfLightCastTime = Time.time;
    }

    private void UpdateMode()
    {
        School = IsLightMode ? Schools.Light : Schools.Dark;
    }

    protected override void CastAction()
    {
        if (_target == null) return;

        if (IsLightMode)
        {
            HandleDefaultMode();
        }
        else
        {
            HandleAlternativeMode();
        }
    }

    private bool IsTargetBelowHealthThreshold(Character target)
    {
        var healthComponent = target.GetComponent<Health>();
        return healthComponent != null && healthComponent.CurrentValue <= healthComponent.MaxValue * LowHealthThreshold;
    }

    private void HandleDefaultMode()
    {
        if (IsAllyTarget(_target) && TryPayCost(_manaCostHeal))
        {
            Heal(_target);
            ApplySpiritEnergyBuff(_target);
            //ApplyHealthBuff(_target);
        }
        else if (IsEnemyTarget(_target) && TryPayCost(_manaCostDamage))
        {
            DamageCast(_target);
        }
    }

    private void HandleAlternativeMode()
    {
        if (IsEnemyTarget(_target) && TryPayCost(_altManaCostDamage))
        {
            ApplyDamageInAltMode(_target);
            ApplySpiritHealthBuff(_target);
            
            if (_lowHealthTalentActive && IsTargetBelowHealthThreshold(_target))
            {
                ApplyDefenseDebuff(_target);
            }
        }
    }

    private void Heal(Character target)
    {
        var isBonusActive = _healingBuffTalentActive && Time.time < _lastFlashOfLightCastTime + _healingBuffDuration;
        
        if (isBonusActive)
        {
            _healingBonusStacks++;
        }
        else
        {
            _healingBonusStacks = 0;
        }
        
        var bonus = isBonusActive ? _tickHealingBonus * _healingBonusStacks : 0;
        
        var heal = new Heal { Value = _healAmount + bonus };
        CmdApplyHeal(heal, target.gameObject, this, Name);
    }

    private void DamageCast(Character target)
    {
        CmdApplyDamage(CreateDamage(_damageAmount), target.gameObject);
    }

    private void ApplyDamageInAltMode(Character target)
    {
        float damageAmount = _altDamageAmount;
        if (_lowHealthTalentActive && IsTargetBelowHealthThreshold(target))
        {
            damageAmount *= BonusDamageMultiplier;
        }
        
        CmdApplyDamage(CreateDamage(damageAmount), target.gameObject);
    }

    private Damage CreateDamage(float amount)
    {
        return new Damage
        {
            Value = Buff.Damage.GetBuffedValue(amount),
            Type = DamageType.Magical,
            PhysicAttackType = AttackRangeType.RangeAttack,
            School = this.School,
            DamageableSkill = this,
        };
    }

    private void ApplySpiritEnergyBuff(Character target)
    {
        var talentActive = _manaRestoreBoostTalent ? 1 : 0;
        CmdAddBuff(States.SpiritEnergy, _buffDuration, talentActive, target.gameObject, Name);
    }

    private void ApplySpiritHealthBuff(Character target)
    {
        var talentActive = _manaRestoreBoostTalent ? 1 : 0;
        CmdAddBuff(States.SpiritHealth, _altBuffDuration, talentActive, target.gameObject, Name);
    }

    private void ApplyHealthBuff(Character target)
    {
        if (!_healthBoostActive) return;

        CmdAddBuff(States.SparkTalentHealthBuff, HealthBoostDuration, HealthBoostPercentage, target.gameObject, Name);
    }

    private void ApplyDefenseDebuff(Character target)
    {
        CmdAddBuff(States.DefenseReduction, DefenseDebuffDuration, DefenseReductionPercentage, target.gameObject, Name);
    }

    [Command]
    private void CmdAddBuff(States state, float duration, float modifier, GameObject target, string skillName)
    {
        var characterState = target.GetComponent<CharacterState>();
        characterState.AddState(state, duration, modifier, target, skillName);
    }

    protected override void ClearData()
    {
        base.ClearData();
    }
}