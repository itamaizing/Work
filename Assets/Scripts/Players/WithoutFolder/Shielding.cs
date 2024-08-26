using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public class Shielding
{
    protected HealthComponent _healthComponent;
    protected float _shieldAmount;

    public DamageType DamageType;

    public Shielding(HealthComponent healthComponent, float shieldValue, DamageType damageType)
    {
        _shieldAmount = shieldValue;
        _healthComponent = healthComponent;
        DamageType = damageType;
        AddShieldBehavior(healthComponent, damageType);
    }

    protected void AddShieldBehavior(HealthComponent healthComponent, DamageType damageType)
    {
        healthComponent.AddShieldBehavior(this, damageType);
    }

    public virtual float GetShieldAmount(GameObject obj)
    {
        return _shieldAmount;
    }

    public virtual void RemoveAmount(float amount) 
    {
        _shieldAmount -= amount;
    }
}
