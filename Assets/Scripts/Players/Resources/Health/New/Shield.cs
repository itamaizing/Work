using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shield : Resource, IDamageable
{
    private DamageType _absorptionDamageType;
    private float _percentageAbsorption = 1;
    private bool _isBreaksDown = true;

    public event Action<float, DamageType> DamageTaked;

    public void Initialize(float maxValue, DamageType damageType, float percentageAbsorption = 1, bool isBreaksDown = true, float regenValue = 0, float regenDelay = 0)
    {
        base.Initialize(maxValue, regenValue, regenDelay);

        _absorptionDamageType = damageType;
        _percentageAbsorption = percentageAbsorption;
        _isBreaksDown = isBreaksDown;
    }

    public bool TryTakeDamage(ref Damage damage, Skill skill)
    {
        if(_absorptionDamageType == DamageType.Both || _absorptionDamageType == damage.Type)
        {
            float absorptionDamage = damage.Value * _percentageAbsorption;
            float remainingDamage = damage.Value - CurrentValue;

            if (TryUse(absorptionDamage))
            {
                DamageTaked?.Invoke(absorptionDamage, damage.Type);
                damage.Value = damage.Value - absorptionDamage;
                return true;
            }
            else
            {
                DamageTaked?.Invoke(damage.Value - remainingDamage, damage.Type);
                damage.Value = remainingDamage;

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
