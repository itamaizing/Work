using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Shielding
{
    protected HealthComponent HealthComponent;
    public DamageType DamageType;
    public float shieldAmount;

    public Shielding(HealthComponent healthComponent, float shieldValue, DamageType damageType)
    {
        shieldAmount = shieldValue;
        HealthComponent = healthComponent;
        DamageType = damageType;
        AddShieldBehavior(healthComponent, damageType);
    }

    protected void AddShieldBehavior(HealthComponent healthComponent, DamageType damageType)
    {
        healthComponent.AddShieldBehavior(this, damageType);
    }

}
