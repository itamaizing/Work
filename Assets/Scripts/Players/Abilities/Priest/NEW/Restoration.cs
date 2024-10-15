using System;
using System.Collections;
using UnityEngine;

public class Restoration : Skill
{
    [Header("Restoration (Light Mode) Settings")]
    [SerializeField] private float healPerTick = 6f;
    [SerializeField] private float lightRange = 4f;
    [SerializeField] private float lightDuration = 12.1f;
    [SerializeField] private float healInterval = 4f;
    [SerializeField] private float lightCastTime = 1.2f;
    [SerializeField] private float effectivenessIncreasePerHeal = 0.1f;

    [Header("Restoration (Dark Mode) Settings")]
    [SerializeField] private float damagePerTick = 6f;
    [SerializeField] private float darkRange = 6f;
    [SerializeField] private float darkDuration = 12.1f;
    [SerializeField] private float damageInterval = 3f;
    [SerializeField] private float darkCastTime = 1.2f;

    public bool isLightMode = true;
    private Character _target;
    private float _accumulatedEffectiveness = 1f;
    private float _totalHealedInInterval = 0f;
    
    protected override bool IsCanCast => IsCanCastCheck();

    private bool IsCanCastCheck()
    {
        if (_target == null) return false;
        return Vector3.Distance(transform.position, _target.transform.position) <= Radius;
    }

    public event Action OnModeChange;

    private void OnEnable()
    {
        OnModeChange += HandleModeChange;
        UpdateMode();
    }

    private void OnDisable()
    {
        OnModeChange -= HandleModeChange;
        if (_target != null)
        {
            var healthComponent = _target.GetComponent<Health>();
            if (healthComponent != null)
            {
                healthComponent.HealTaked -= OnHealTaken;
            }
        }
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
        Radius = isLightMode ? lightRange : darkRange;
        School = isLightMode ? Schools.Light : Schools.Dark;
        CastDeley = isLightMode ? lightCastTime : darkCastTime;
        TargetsLayers = isLightMode ? LayerMask.GetMask("Allies") : LayerMask.GetMask("Enemy");
    }

    private void HandleRestorationLight()
    {
        if (_target == null) return;

        bool isAlly = _target.gameObject.layer == LayerMask.NameToLayer("Allies");

        if (isAlly && TryPayCost())
        {
            var healthComponent = _target.GetComponent<Health>();
            if (healthComponent != null)
            {
                healthComponent.HealTaked += OnHealTaken;
            }

            StartCoroutine(ApplyHealOverTime(_target));
        }
    }

    private void HandleRestorationDark()
    {
        if (_target == null) return;

        bool isEnemy = _target.gameObject.layer == LayerMask.NameToLayer("Enemy");

        if (isEnemy && TryPayCost())
        {
            StartCoroutine(ApplyDamageOverTime(_target));
        }
    }
    
    private void OnHealTaken(float healedAmount, Skill skill, string sourceName)
    {
        _totalHealedInInterval += healedAmount;
    }

    private IEnumerator ApplyHealOverTime(Character target)
    {
        var healthComponent = target.GetComponent<Health>();

        if (healthComponent != null)
        {
            float endTime = Time.time + lightDuration;
            while (Time.time < endTime)
            {
                float effectiveHeal = healPerTick * _accumulatedEffectiveness;
                
                var heal = new Heal { Value = effectiveHeal };
                ApplyHeal(heal, healthComponent.gameObject, name);
                
                _accumulatedEffectiveness += _totalHealedInInterval * effectivenessIncreasePerHeal;
                
                _totalHealedInInterval = 0f;

                yield return new WaitForSeconds(healInterval);
            }
            healthComponent.HealTaked -= OnHealTaken;
        }
    }

    private IEnumerator ApplyDamageOverTime(Character target)
    {
        var healthComponent = target.GetComponent<Health>();

        if (healthComponent != null)
        {
            float endTime = Time.time + darkDuration;
            while (Time.time < endTime)
            {
                Damage damage = new Damage
                {
                    Value = Buff.Damage.GetBuffedValue(damagePerTick),
                    Type = DamageType.Magical,
                    PhysicAttackType = AttackRangeType.RangeAttack
                };
                
                ApplyDamage(damage, target.gameObject);
                yield return new WaitForSeconds(damageInterval);
            }
        }
    }

    protected override IEnumerator PrepareJob()
    {
        while (_target == null)
        {
            if (Input.GetMouseButton(0))
            {
                _target = GetRaycastTarget();
            }
            yield return null;
        }
    }

    protected override IEnumerator CastJob()
    {
        if (_target == null) yield break;

        if (isLightMode)
        {
            HandleRestorationLight();
        }
        else
        {
            HandleRestorationDark();
        }

        yield return null;
    }

    protected override void ClearData()
    {
        _target = null;
        ResetAccumulatedEffectiveness();
    }

    private void ResetAccumulatedEffectiveness()
    {
        _accumulatedEffectiveness = 1f;
    }
}