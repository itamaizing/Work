using System;
using System.Collections.Generic;
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
    
    public bool isLightMode = true;

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
        isLightMode = !isLightMode;
        OnModeChange?.Invoke();
    }
    
    private void HandleModeChange()
    {
        UpdateMode();
    }
    
    private void UpdateMode()
    {
        School = isLightMode ? Schools.Light : Schools.Dark;
    }
    
    protected override void CastAction()
    {
        if (_target == null) return;

        if (isLightMode)
        {
            HandleDefaultMode();
        }
        else
        {
            HandleAlternativeMode();
        }
    }

    private void HandleDefaultMode()
    {
        bool isAlly = _target.gameObject.layer == LayerMask.NameToLayer("Allies");
        bool isEnemy = _target.gameObject.layer == LayerMask.NameToLayer("Enemy");

        if (isAlly && TryPayCost(_manaCostHeal))
        {
            Heal(_target);
            ApplySpiritEnergyBuff(_target);
        }
        else if (isEnemy && TryPayCost(_manaCostDamage))
        {
            Damage(_target);
        }
    }

    private void HandleAlternativeMode()
    {
        bool isEnemy = _target.gameObject.layer == LayerMask.NameToLayer("Enemy");

        if (isEnemy && TryPayCost(_altManaCostDamage))
        {
            ApplyDamageInAltMode(_target);
            ApplySpiritHealthBuff(_target);
        }
    }

    private void Heal(Character target)
    {
        var healthComponent = target.GetComponent<Health>();
        if (healthComponent != null)
        {
            healthComponent.Heal(_healAmount);
        }
    }

    private void Damage(Character target)
    {
        Damage damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(_damageAmount),
            Type = DamageType.Magical,
            Range = AttackRangeType.RangeAttack
        };

        CmdApplyDamage(damage, target.gameObject);
    }

    private void ApplyDamageInAltMode(Character target)
    {
        Damage damage = new Damage
        {
            Value = _altDamageAmount,
            Type = DamageType.Magical,
            Range = AttackRangeType.RangeAttack
        };

        CmdApplyDamage(damage, target.gameObject);
    }

    private void ApplySpiritEnergyBuff(Character target)
    {
        if (target.TryGetComponent<CharacterState>(out var characterState))
        {
            Debug.LogError("fix state");

            characterState.CmdAddState(States.SpiritEnergy, _buffDuration, 0, target.gameObject, "SparkOfLight");
        }
    }

    private void ApplySpiritHealthBuff(Character target)
    {
        if (target.TryGetComponent<CharacterState>(out var characterState))
        {
            characterState.CmdAddState(States.SpiritHealth, _altBuffDuration, 0, target.gameObject, "SparkOfLight");
        }
    }

    protected override void ClearData()
    {
        base.ClearData();
    }
}