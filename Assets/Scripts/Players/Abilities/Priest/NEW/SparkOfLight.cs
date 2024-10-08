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

    public bool IsLightMode { get; private set; } = true;

    private bool _healthBoostActive = false;
    private bool _lowHealthTalentActive = false;
    private const float LowHealthThreshold = 0.25f;
    private const float BonusDamageMultiplier = 1.25f;
    private const float HealthBoostPercentage = 0.25f;
    private const float HealthBoostDuration = 2f;
    private const float DefenseReductionPercentage = 0.25f;
    private const float DefenseDebuffDuration = 2f;

    public void EnableTalentPhysicalShieldBoost(bool value) => _healthBoostActive = value;
    public void EnableLowHealthTalent(bool value) => _lowHealthTalentActive = value;
    
    private bool IsAllyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");
    private bool IsEnemyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Enemy");
    
    public event Action OnModeChange;

    private void OnEnable()
    {
        OnModeChange += HandleModeChange;
        UpdateMode();
    }

    private void OnDisable()
    {
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
            ApplyHealthBuff(_target);
        }
        else if (IsEnemyTarget(_target) && TryPayCost(_manaCostDamage))
        {
            Damage(_target);
        }
    }

    private void HandleAlternativeMode()
    {
        if (IsEnemyTarget(_target) && TryPayCost(_altManaCostDamage))
        {
            ApplyDamageInAltMode(_target);
            if (_lowHealthTalentActive && IsTargetBelowHealthThreshold(_target))
            {
                ApplyDefenseDebuff(_target);
            }
        }
    }

    private void Heal(Character target)
    {
        var healthComponent = target.GetComponent<Health>();
        if (healthComponent != null)
        {
            var heal = new Heal { Value = _healAmount };
            healthComponent.Heal(ref heal);
        }
    }

    private void Damage(Character target)
    {
        ApplyDamage(CreateDamage(_damageAmount), target.gameObject);
    }

    private void ApplyDamageInAltMode(Character target)
    {
        float damageAmount = _altDamageAmount;
        if (_lowHealthTalentActive && IsTargetBelowHealthThreshold(target))
        {
            damageAmount *= BonusDamageMultiplier;
        }

        ApplyDamage(CreateDamage(damageAmount), target.gameObject);
    }

    private Damage CreateDamage(float amount)
    {
        return new Damage
        {
            Value = Buff.Damage.GetBuffedValue(amount),
            Type = DamageType.Magical,
            PhysicAttackType = AttackRangeType.RangeAttack
        };
    }

    private void ApplySpiritEnergyBuff(Character target)
    {
        CmdAddBuff(States.SpiritEnergy, _buffDuration, 0, target.gameObject, name);
    }

    private void ApplyHealthBuff(Character target)
    {
        if (!_healthBoostActive) return;

        CmdAddBuff(States.SparkTalentHealthBuff, HealthBoostDuration, HealthBoostPercentage, target.gameObject, name);
    }

    private void ApplyDefenseDebuff(Character target)
    {
        CmdAddBuff(States.DefenseReduction, DefenseDebuffDuration, DefenseReductionPercentage, target.gameObject, name);
    }

    [Command]
    private void CmdAddBuff(States state, float duration, float modifier, GameObject target, string skillName)
    {
        var characterState = target.GetComponent<CharacterState>();
        characterState?.AddState(state, duration, modifier, target, skillName);
    }

    protected override void ClearData()
    {
        base.ClearData();
    }
}