using System;

public class Shield : Resource, IDamageable
{
    protected DamageType _absorptionDamageType;
    protected float _percentageAbsorption = 1;
    protected bool _isBreaksDown = true;

    public event Action<float, DamageType, Skill> DamageTaken;

    public void Initialize(float maxValue, DamageType damageType, float percentageAbsorption = 1, bool isBreaksDown = true, float regenValue = 0, float regenDelay = 0)
    {
        _currentValue = maxValue;
        _maxValue = maxValue;
        _regenerationValue = regenValue;
        _regenerationDelay = regenDelay;

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
        if(_absorptionDamageType == DamageType.Both || _absorptionDamageType == damage.Type)
        {
            float absorptionDamage = damage.Value * _percentageAbsorption;
            float remainingDamage = damage.Value - CurrentValue;

            if (TryUse(absorptionDamage))
            {
                DamageTaken?.Invoke(absorptionDamage, damage.Type, skill);
                damage.Value = damage.Value - absorptionDamage;
                return true;
            }
            else
            {
                DamageTaken?.Invoke(damage.Value - remainingDamage, damage.Type, skill);
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
