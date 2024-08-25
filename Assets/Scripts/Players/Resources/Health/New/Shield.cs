using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shield : Resource, IDamageable
{
    private DamageType _absorptionDamageType;
    private float _percentageAbsorption;
    private bool _isBreaksDown;

    public event Action<float, DamageType> DamageTaked;

    public void Initialize(float maxValue, DamageType damageType, float percentageAbsorption = 1, bool isBreaksDown = true, float regenValue = 0, float regenDelay = 0)
    {
        base.Initialize(maxValue, regenValue, regenDelay);

        _absorptionDamageType = damageType;
        _percentageAbsorption = percentageAbsorption;
        _isBreaksDown = isBreaksDown;
    }

    public bool TryTakeDamage(ref float damage, IDamageDealer skill)
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

                if (_isBreaksDown)
                    Destroy(this.gameObject);

                return true;
            }
        }
        else
        {
            return false;
        }
    }
}
