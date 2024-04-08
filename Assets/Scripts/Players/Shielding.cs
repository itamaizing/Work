using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Shielding
{
    protected HealthPlayer _healthPlayer;
    public DamageType DamageType;
    public float shieldAmount;

    public Shielding(HealthPlayer healthPlayer, float shieldValue, DamageType damageType)
    {
        shieldAmount = shieldValue;
        _healthPlayer = healthPlayer;
        DamageType = damageType;
        AddShieldBehavior(healthPlayer, damageType);
    }

    protected void AddShieldBehavior(HealthPlayer healthPlayer, DamageType damageType)
    {
        healthPlayer.AddShieldBehavior(this, damageType);
    }

}
