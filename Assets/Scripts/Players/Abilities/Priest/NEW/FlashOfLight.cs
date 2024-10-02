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
    
    protected override bool IsCanCast => IsCanCastCheck();
    
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
        Radius = isLightMode ? _lightRange : _darkRange;
        School = isLightMode ? Schools.Light : Schools.Dark;
        TargetsLayers = isLightMode ? LayerMask.GetMask("Allies") : LayerMask.GetMask("Enemy");
    }

    private bool IsCanCastCheck()
    {
        if (_target == null) return false;
        return Vector3.Distance(transform.position, _target.transform.position) <= Radius;
    }

    private void HandleFlashOfLight()
    {
        if (TryPayCost())
        {
            Heal(_target);
        }
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
            healthComponent.Heal(_healAmount);
        }
    }
    
    private void Damage(Character target)
    {
        Damage damage = new Damage
        {
            Value = _damageAmount,
            Type = DamageType.Magical,
            Range = AttackRangeType.RangeAttack
        };

        CmdApplyDamage(damage, target.gameObject);
    }
    
    protected override IEnumerator PrepareJob()
    {
        while (_target == null)
        {
            if (Input.GetMouseButton(0))
            {
                _target = GetRaycastTarget(true);
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
