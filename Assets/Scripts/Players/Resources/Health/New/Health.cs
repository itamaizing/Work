using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    public bool TryTakeDamage(ref float damage, IDamageDealer skill)
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
        _sumDamageTaken += damage;
        return true;
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

    protected void UseShields(ref float damage, IDamageDealer skill)
    {
        for(int i = 0; i < _shields.Count; i++)
        {
            if (_shields[i] != null)
            {
                _shields[i].TryTakeDamage(ref damage, skill);
                if (damage == 0)
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
        DamageTaked?.Invoke(damageTaken, damageType);
    }

    [ClientRpc]
    private void ClientRpcDied()
    {
        Died?.Invoke();
    }

    #region Test

    [SerializeField] Skill Testskill1;
    [SerializeField] float TestDamage;
    [SerializeField] Shield Shield;

    [ContextMenu("CmdAddShield")]
    private void TestAddShield()
    {
        CmdTestAddShield();
    }

    [Command]
    private void CmdTestAddShield()
    {
        var shield = Instantiate(Shield);

        shield.Initialize(100, DamageType.Magical, 1, true, 10, 1);
        NetworkServer.Spawn(shield.gameObject, gameObject);

        Shields.Add(shield);
        ClientRpcTestAddShield(shield.gameObject);
    } 

    [ClientRpc]
    private void ClientRpcTestAddShield(GameObject gameObject)
    {
        var shield = gameObject.GetComponent<Shield>();
        shield.Initialize(100, DamageType.Magical, 1, true, 10, 1);
        Shields.Add(shield);
    }

    [ContextMenu("CmdTakeDamage")]
    [Command]
    private void CmdTestTakeDamage()
    {
        TryTakeDamage(ref TestDamage, Testskill1);
    }

    #endregion
}
