using System;

public class Shield : Resource, IDamageable
{
    protected DamageType _absorptionDamageType;
    protected float _percentageAbsorption = 1;
    protected bool _isBreaksDown = true;

    public event Action<float, Damage, Skill> DamageTaken;

    public void Initialize(float maxValue, DamageType damageType, float percentageAbsorption = 1, bool isBreaksDown = true, float regenValue = 0, float regenDelay = 0)
    {
        _currentValue = maxValue;
        _maxValue = maxValue;
        _regenerationValue = regenValue;
        _regenerationPeriod = regenDelay;

        if (regenValue > 0)
            ClientStartRegenirateJob();

        _absorptionDamageType = damageType;
        _percentageAbsorption = percentageAbsorption;
        _isBreaksDown = isBreaksDown;
    }

    public void ShowPhantomValue(Damage phantomValue)
    {
        throw new NotImplementedException();
    }

    public bool TryTakeDamage(ref Damage damage, Skill skill)
    {
        if (_absorptionDamageType == DamageType.Both || _absorptionDamageType == damage.Type)
        {
            float absorptionDamage = damage.Value * _percentageAbsorption;
            float remainingDamage = damage.Value - CurrentValue;

            if (TryUse(absorptionDamage))
            {
                var tempDamage = new Damage
                {
                    Form = damage.Form,
                    PhysicAttackType = damage.PhysicAttackType,
                    School = damage.School,
                    Type = damage.Type,
                    Value = absorptionDamage,
                };

                DamageTaken?.Invoke(tempDamage.Value, tempDamage, skill);
                damage.Value = damage.Value - absorptionDamage;
                return true;
            }
            else
            {
                var tempDamage = new Damage
                {
                    Form = damage.Form,
                    PhysicAttackType = damage.PhysicAttackType,
                    School = damage.School,
                    Type = damage.Type,
                    Value = damage.Value - remainingDamage,
                };

                DamageTaken?.Invoke(tempDamage.Value, tempDamage, skill);
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