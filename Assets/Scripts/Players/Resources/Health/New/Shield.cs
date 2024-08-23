using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shield : Resource, IDamageable
{
    private DamageType _absorptionDamageType;
    private float _percentageAbsorption;

    public event Action<float, DamageType> DamageTaked;

    public void Initialize(float maxValue, float regenValue, float regenDelay, DamageType damageType, float percentageAbsorption = 1)
    {
        base.Initialize(maxValue, regenValue, regenDelay);

        _absorptionDamageType = damageType;
        _percentageAbsorption = percentageAbsorption;
    }

    public bool TryTakeDamage(ref float damage, Skill skill)
    {
        if(_absorptionDamageType == DamageType.Both || _absorptionDamageType == skill.DamageType)
        {
            float absorptionDamage = damage * _percentageAbsorption;
            float remainingDamage = damage - CurrentValue;

            if (TryUse(absorptionDamage))
            {
                DamageTaked?.Invoke(absorptionDamage, skill.DamageType);
                damage = damage - absorptionDamage;
                return true;
            }
            else
            {
                DamageTaked?.Invoke(damage - remainingDamage, skill.DamageType);
                damage = remainingDamage;
                return true;
            }
        }
        else
        {
            return false;
        }
    }
}
