using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Health : Resource, IDamageable, IHealingable
{
    [SyncVar(hook = nameof(HookEvadeMeleeDamageChanged))] protected float _evadeMeleeDamage;
    [SyncVar(hook = nameof(HookEvadeRangeDamageChanged))] protected float _evadeRangeDamage;
    [SyncVar(hook = nameof(HookEvadeMagDamageChanged))] protected float _evadeMagDamage;
    [SyncVar(hook = nameof(HookDefPhysDamageChanged))] protected float _defPhysDamage;
    [SyncVar(hook = nameof(HookDefMagDamageChanged))] protected float _defMagDamage;

    private List<IDamageable> _shields = new List<IDamageable>();
    private float _sumDamageTaken = 0;

    public float SumDamageTaken { get => _sumDamageTaken; }
    public float EvadeMeleeDamage { get => _evadeMeleeDamage; set => _evadeMeleeDamage = value; }
    public float EvadeRangeDamage { get => _evadeRangeDamage; set => _evadeRangeDamage = value; }
    public float EvadeMagDamage { get => _evadeMagDamage; set => _evadeMagDamage = value; }
    public float DefPhysDamage { get => _defPhysDamage; set => _defPhysDamage = value; }
    public float DefMagDamage { get => _defMagDamage; set => _defMagDamage = value; }
    public List<IDamageable> Shields { get => _shields; }

    public event Action Evaded;
    public event Action<float> HealTaked;
    public event Action<float, DamageType> DamageTaken;
    public event Action Died;

    public event Action<float, float> EvadeMeleeDamageChanged;
    public event Action<float, float> EvadeRangeDamageChanged;
    public event Action<float, float> EvadeMagDamageChanged;
    public event Action<float, float> DefPhysDamageChanged;
    public event Action<float, float> DefMagDamageChanged;

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
        ClientRpcDamageTaked(damage.Value, damage.Type);
        _sumDamageTaken += damage.Value;
        return true;
    }

    [Command(requiresAuthority = false)]
    public void CmdTryTakeDamage(Damage damage, GameObject skillCanBeNull)
    {
        TryTakeDamage(ref damage, null);
    }

    public void Heal(float value)
    {
        Add(value);
        HealTaked?.Invoke(value);
    }

    public void SetEvadeMagic(float value)
    {
        _evadeMagDamage = value;
    }

    public void SetDefMagicDamage(float value)
    {
        _defMagDamage = value;
    }

    #region HookMethods

    protected virtual void HookEvadeMeleeDamageChanged(float oldValue, float newValue)
    {
        EvadeMeleeDamageChanged?.Invoke(oldValue, newValue);
    }

    protected virtual void HookEvadeRangeDamageChanged(float oldValue, float newValue)
    {
        EvadeRangeDamageChanged?.Invoke(oldValue, newValue);
    }

    protected virtual void HookEvadeMagDamageChanged(float oldValue, float newValue)
    {
        EvadeMagDamageChanged?.Invoke(oldValue, newValue);
    }

    protected virtual void HookDefPhysDamageChanged(float oldValue, float newValue)
    {
        DefPhysDamageChanged?.Invoke(oldValue, newValue);
    }

    protected virtual void HookDefMagDamageChanged(float oldValue, float newValue)
    {
        DefMagDamageChanged?.Invoke(oldValue, newValue);
    }

    #endregion

    protected bool TryEvade(DamageType damageType, AttackRangeType attackRangeType)
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
    private void ClientRpcDamageTaked(float damageTaken, DamageType damageType)
    {
        DamageTaken?.Invoke(damageTaken, damageType);
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
}