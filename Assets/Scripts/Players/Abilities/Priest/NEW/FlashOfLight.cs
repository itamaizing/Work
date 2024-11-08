using System;
using System.Collections;
using UnityEngine;

public class FlashOfLight : Skill
{
    [Header("Flash of Light Settings")] 
    [SerializeField] private float _healAmount = 35f;
    [SerializeField] private float _lightRange = 4f;
    
    [Header("Flash of Darkness Settings")] 
    [SerializeField] private float _damageAmount = 35f;
    [SerializeField] private float _darkRange = 6f;

    public bool isLightMode = true;

    private Character _target;
    private Character _previousTarget;

    private bool _isСooldownTalentActive = false;
    private float _talentCooldown = 5f;
    private float _lastTalentTime = -5f;
    private float _cooldownReduction = 5f;
    
    public event Action OnModeChange;
    
    protected override bool IsCanCast => IsCanCastCheck();

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    
    private bool IsCanCastCheck()
    {
        if (_target == null) return false;
        return Vector3.Distance(transform.position, _target.transform.position) <= Radius;
    }
    
    private bool IsNewTarget => _previousTarget != _target;
    
    public void EnableTalentPhysicalShieldBoost(bool value)
    {
        _isСooldownTalentActive = value;
    }
    
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
        Radius = isLightMode ? _lightRange : _darkRange;
        School = isLightMode ? Schools.Light : Schools.Dark;
        TargetsLayers = isLightMode ? LayerMask.GetMask("Allies") : LayerMask.GetMask("Enemy");
    }
    
    private void HandleFlashOfLight()
    {
        if (IsNewTarget && _isСooldownTalentActive && Time.time - _lastTalentTime >= _talentCooldown)
        {
            ReduceCooldowns();
            _lastTalentTime = Time.time;
        }
        

        if (!IsNewTarget || TryPayCost())
        {
            Heal(_target);
        }

        _previousTarget = _target;
    }
    
    private void HandleFlashOfDarkness()
    {
        if (TryPayCost())
        {
            Damage(_target);
        }
    }
    
    private void Heal(Character target)
    {
        var healthComponent = target.GetComponent<Health>();
        if (healthComponent != null)
        {
            var heal = new Heal { Value = _healAmount };
            CmdApplyHeal(heal, healthComponent.gameObject, this, name);
        }
    }
    
    private void Damage(Character target)
    {
        Damage damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(_damageAmount),
            Type = DamageType.Physical,
            PhysicAttackType = AttackRangeType.RangeAttack,
            School = this.School,
            //DamageableSkill = this,
        };

        CmdApplyDamage(damage, target.gameObject);
    }
    
    private void ReduceCooldowns()
    {
        foreach (var ability in Hero.Abilities.Abilities)
        {
            ability.DecreaseSetCooldown(_cooldownReduction);
        }
    }
    
    protected override IEnumerator PrepareJob()
    {
        while (_target == null)
        {
            if (Input.GetMouseButton(0))
            {
                _target = GetTarget().character;
            }
            yield return null;
        }
    }

    protected override IEnumerator CastJob()
    {
        if (_target == null) yield break;

        if (isLightMode)
        {
            HandleFlashOfLight();
        }
        else if (!isLightMode)
        {
            HandleFlashOfDarkness();
        }

        yield return null;
    }

    protected override void ClearData()
    {
        _target = null;
    }
}
