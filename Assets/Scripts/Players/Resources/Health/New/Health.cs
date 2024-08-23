using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health : Resource, IDamageable
{
    [SerializeField] private bool _isCanHaveOverHeal;
    [SerializeField] private List<IDamageable> _shields = new List<IDamageable>();

    protected float _evadeMeleeDamage;
    protected float _evadeRangeDamage;
    protected float _defPhysDamage;
    protected float _evadeMagDamage;
    protected float _defMagDamage;

    private float _sumDamageTaken = 0;

    public float SumDamageTaken { get => _sumDamageTaken; }
    public float EvadeMeleeDamage { get => _evadeMeleeDamage; }
    public float EvadeRangeDamage { get => _evadeRangeDamage; }
    public float DefPhysDamage { get => _defPhysDamage; }
    public float EvadeMagDamage { get => _evadeMagDamage; }
    public float DefMagDamage { get => _defMagDamage; }
    public bool IsCanHaveOverHeal { get => _isCanHaveOverHeal; }
    public List<IDamageable> Shields { get => _shields; }

    public event Action Evaded;
    public event Action<float> HealTaked;
    public event Action<float, DamageType> DamageTaked;
    public event Action Died;

    public void Initialize(float maxHealth, float regenValue, float regenDelay, HealthInfo healthInfo)
    {
        base.Initialize(maxHealth, regenValue, regenDelay);

        _defPhysDamage = healthInfo.DefaultPhysicsDamage;
        _defMagDamage = healthInfo.DefaultMagicDamage;
        _evadeMagDamage = healthInfo.EvadeMagicDamage;
        _evadeMeleeDamage = healthInfo.EvadeMeleeDamage;
        _evadeRangeDamage = healthInfo.EvadeRangeDamage;
    }

    public bool TryTakeDamage(ref float damage, Skill skill)
    {
        if (TryEvade(skill.DamageType, skill.AttackRangeType))
        {
            Evaded?.Invoke();
            return false;
        }
        UseShields(ref damage, skill);

        if (damage == 0)
            return true;

        if (TryUse(damage) == false)
        {
            ClientRpcDied();
        }
        ClientRpcDamageTaked(damage, skill.DamageType);
        return true;
    }

    protected bool TryEvade(DamageType damageType, AttackRangeType attackRangeType)
    {
        switch (damageType)
        {
            case DamageType.Magical:

                if (UnityEngine.Random.Range(0, 100) > _evadeMagDamage)
                    return true;
                else
                    return false;

                break;

            case DamageType.Physical:
                switch (attackRangeType)
                {
                    case AttackRangeType.MeleeAttack:

                        if (UnityEngine.Random.Range(0, 100) > _evadeMeleeDamage)
                            return true;

                        else
                            return false;

                        break;

                    case AttackRangeType.RangeAttack:

                        if (UnityEngine.Random.Range(0, 100) > _evadeRangeDamage)
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

    protected void UseShields(ref float damage, Skill skill)
    {
        foreach (var item in _shields)
        {
            item.TryTakeDamage(ref damage, skill);

            if (damage == 0)
            {
                break;
            }
        }
    }

    [ClientRpc]
    private void ClientRpcDamageTaked(float damageTaken, DamageType damageType)
    {
        DamageTaked?.Invoke(damageTaken, damageType);
    }

    [ClientRpc]
    private void ClientRpcDied()
    {
        Died?.Invoke();
    }
}
