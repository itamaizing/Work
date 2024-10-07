using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Health : Resource, IDamageable, IHealingable
{
    protected float _evadeMeleeDamage;
    protected float _evadeRangeDamage;
    protected float _defPhysDamage;
    protected float _evadeMagDamage;
    protected float _defMagDamage;

    private List<IDamageable> _shields = new List<IDamageable>();
    private float _sumDamageTaken = 0;

    public float SumDamageTaken { get => _sumDamageTaken; }
    public float EvadeMeleeDamage { get => _evadeMeleeDamage; }
    public float EvadeRangeDamage { get => _evadeRangeDamage; }
    public float DefPhysDamage { get => _defPhysDamage; }
    public float EvadeMagDamage { get => _evadeMagDamage; }
    public float DefMagDamage { get => _defMagDamage; }
    public List<IDamageable> Shields { get => _shields; }

    public event Action Evaded;
    public event Action<float, Skill> HealTaked;
    public event Action<float, DamageType, Skill> DamageTaken;
    public event Action Died;

    public override void Initialize(float health, float hpRegen, float hpRegenDelay, CharacterData data)
    {
        base.Initialize(health, hpRegen, hpRegenDelay, data);

        _defPhysDamage = data.GetAttributeValue(AttributeNames.PhysicResist);
        _defMagDamage = data.GetAttributeValue(AttributeNames.MagicResist);
        _evadeMagDamage = data.GetAttributeValue(AttributeNames.MagicEvade);
        _evadeMeleeDamage = data.GetAttributeValue(AttributeNames.MeleeEvade);
        _evadeRangeDamage = data.GetAttributeValue(AttributeNames.RangeEvade);
    }

    public bool TryTakeDamage(ref Damage damage, Skill skill)
    {
        if (TryEvade(damage.Type, damage.Range))
        {
            Evaded?.Invoke();
            return false;
        }
        UseShields(ref damage, skill);

        if (damage.Value == 0)
            return true;

        if (TryUse(damage.Value) == false)
        {
            ClientRpcDied();
            Died?.Invoke();
        }
        ClientRpcDamageTaked(damage.Value, damage.Type, skill);
        _sumDamageTaken += damage.Value;
        return true;
    }

    [Command(requiresAuthority = false)]
    public void CmdTryTakeDamage(Damage damage, GameObject skillCanBeNull)
    {
        TryTakeDamage(ref damage, null);
    }

    public void SetHp(float hp, float maxHp)
    {
        _currentValue = hp;
        _maxValue = maxHp;
    }

	public void Heal(float value, Skill skill = null)
    {
        Add(value);
        HealTaked?.Invoke(value, skill);
    }

    public void SetEvadeMagic(float value)
    {
        _evadeMagDamage = value;
    }

    public void SetEvadeAll(float value)
    {
        _evadeMagDamage += value;
        _evadeMeleeDamage += value;
        _evadeRangeDamage += value;
    }    


	public bool TryEvade(DamageType damageType, AttackRangeType attackRangeType)
    {
        switch (damageType)
        {
            case DamageType.Magical:

                if (UnityEngine.Random.Range(0, 100) <= _evadeMagDamage)
                    return true;
                else
                    return false;

                break;

            case DamageType.Physical:
                switch (attackRangeType)
                {
                    case AttackRangeType.MeleeAttack:

                        if (UnityEngine.Random.Range(0, 100) <= _evadeMeleeDamage)
                            return true;

                        else
                            return false;

                        break;

                    case AttackRangeType.RangeAttack:

                        if (UnityEngine.Random.Range(0, 100) <= _evadeRangeDamage)
                            return true;
                        else
                            return false;

                        break;

                    default:
                        break;
                }
                break;

            case DamageType.Both:
                break;

            default:
                return false;
                break;
        }

        return false;
    }

    protected void UseShields(ref Damage damage, Skill skill)
    {
        for (int i = 0; i < _shields.Count; i++)
        {
            if (_shields[i] != null)
            {
                _shields[i].TryTakeDamage(ref damage, skill);

                if (damage.Value == 0)
                {
                    break;
                }
            }
            _shields.RemoveAt(i);
            i--;
        }
    }

    [ClientRpc]
    private void ClientRpcDamageTaked(float damageTaken, DamageType damageType, Skill skill)
    {
        DamageTaken?.Invoke(damageTaken, damageType, skill);
    }

    [ClientRpc]
    private void ClientRpcDied()
    {
        Died?.Invoke();
    }

    public void ResetValue()
    {
        _currentValue = _maxValue;
    }

	public void ShowPhantomValue(Damage phantomValue)
	{
		PhantomValueShow(phantomValue.Value);
	}
}