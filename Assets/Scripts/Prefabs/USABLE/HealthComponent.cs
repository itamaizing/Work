using Mirror;
using Org.BouncyCastle.Bcpg;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(NetworkIdentity))]
public class HealthComponent : NetworkBehaviour
{
    [SerializeField]
    private HealthBar healthBar;
    
    private float _defPhysDamage;
    private float _defMagDamage;

    private float _evadeMeleeDamage;
    private float _evadeRangeDamage;
    private float _evadeMagDamage;

    private float _absorbPhysDamage;
    private float _absorbMagDamage;
    
    private float _hpRegenerationValue;
    private float _hpRegenerationDelay;
    [SyncVar(hook = nameof(NetworkUpdateHealthBar))] 
    private float _currentHealth;

    private float _maxHealth;

    private float _boostRegen = 0;
    private float _boostRegen2 = 0;

    private bool _invinsible = false;

    [Header("Shields")]
    public List<Shielding> shields_Physic = new List<Shielding>();
    public List<Shielding> shields_Magic = new List<Shielding>();
    [FormerlySerializedAs("HealthBar")] [Space]
    
    public float sumDamageTaken = 0;
    public struct DamageInfo
    {
        public float OriginalDamage;
        public float ModifiedDamage;
        public Type CallerType;
    }

    public Action<DamageInfo> OnTakePhisicDamage;
    public Action<DamageInfo> OnTakeMagicDamage;

    public Action<DamageInfo> MakePhisicDamageEvent;
    public Action<DamageInfo> MakeMagicDamageEvent;
    public struct HealInfo
    {
        public float OriginalHeal;
        public float ModifiedHeal;
    }

    public Func<HealInfo, HealInfo> AddHealth;

    public void Initialize(float maxHealth,float regenValue,float regenDelay , HealthInfo healthInfo)
    {
        _currentHealth = maxHealth;
        _maxHealth = maxHealth;
        _hpRegenerationValue = regenValue;
        _hpRegenerationDelay = regenDelay;
        
        _defPhysDamage = healthInfo.DefaultPhysicsDamage;
        _defMagDamage = healthInfo.DefaultMagicDamage;
        _evadeMagDamage = healthInfo.EvadeMagicDamage;
        _evadeMeleeDamage = healthInfo.EvadeMeleeDamage;
        _evadeRangeDamage = healthInfo.EvadeRangeDamage;
        _absorbMagDamage = healthInfo.AbsorbMagicDamage;
        _absorbPhysDamage = healthInfo.AbsorbPhysicsDamage;
        
        UpdateHealthBar();
       // StartCoroutine(CoroutineRegenirateHP());
    }

    public bool TryTakeDamage(float damageValue, DamageType damageType, AttackRangeType attackRangeType)
    {
        float modifiedDamage = CalculateDamageWithStats(damageValue, damageType, attackRangeType, out bool hit);

        if (hit)
        {
            TakeDamage(modifiedDamage, damageType);
        }

        return hit;
    }

    private float CalculateDamageWithStats(float damageValue, DamageType damageType, AttackRangeType attackRangeType, out bool hitSuccessed)
    {
        if (_invinsible)
        {
            hitSuccessed = false;
            return 0;
        }
        if (damageType == DamageType.Magical)
        {
            if (UnityEngine.Random.Range(0, 100) <= _evadeMagDamage)
            {
                ShowDamagePrefab("miss",new Color(120, 120, 120, 1), new Color(120, 120, 120, 0.5f));
                hitSuccessed = false;
                return 0;
            }
            hitSuccessed = true;
            
            damageValue -= (damageValue * _defMagDamage / 100);
            return damageValue - _absorbMagDamage;
        }

        else if (damageType == DamageType.Physical)
        {
            switch (attackRangeType)
            {
                case AttackRangeType.MeleeAttack:
                    if (UnityEngine.Random.Range(0, 100) <= _evadeMeleeDamage)
                    {
                        ShowDamagePrefab("miss",new Color(120, 120, 120, 1), new Color(120, 120, 120, 0.5f));
                        hitSuccessed = false;
                        return 0;
                    }
                    hitSuccessed = true;
                    damageValue -= (damageValue * _defPhysDamage / 100);
                    return damageValue - _absorbPhysDamage;

                case AttackRangeType.RangeAttack:
                    if (UnityEngine.Random.Range(0, 100) <= _evadeRangeDamage)
                    {
                        ShowDamagePrefab("miss",new Color(120, 120, 120, 1), new Color(120, 120, 120, 0.5f));
                        hitSuccessed = false;
                        return 0;
                    }
                    hitSuccessed = true;
                    damageValue -= (damageValue * _defPhysDamage / 100);
                    return damageValue - _absorbPhysDamage;

                case AttackRangeType.Inner:
                    hitSuccessed = true;
                    return damageValue - _absorbPhysDamage;

                default:
                    hitSuccessed = false;
                    return 0; // �� ������� AttackRangeType

            }
        }
        hitSuccessed = false;
        return 0; // �� ������� DamageType
    }
    private float SummShields(DamageType damageType)
    {
        float value = 0;

        if (damageType == DamageType.Physical)
        {
            for (int i = 0; i < shields_Physic.Count; i++)
            {
                if (shields_Physic[i].DamageType == damageType)
                {
                    value += shields_Physic[i].shieldAmount;
                }
            }
        }

        if (damageType == DamageType.Magical)
        {
            for (int i = 0; i < shields_Magic.Count; i++)
            {
                if (shields_Magic[i].DamageType == damageType)
                {
                    value += shields_Magic[i].shieldAmount;
                }
            }
        }

        return value;
    }

    private float CalculateDamageForShields(float damageValue, DamageType damageType)
    {
        if (damageType == DamageType.Physical)
        {
            if (SummShields(damageType) > damageValue)
            {
                for (int i = shields_Physic.Count - 1; i >= 0; i--)
                {
                    Shielding shield = shields_Physic[i];
                    if (damageValue >= shield.shieldAmount)
                    {
                        damageValue -= shield.shieldAmount;
                        shield.shieldAmount = 0;
                        shields_Physic.Remove(shield);
                    }
                    else
                    {
                        shield.shieldAmount -= damageValue;
                        return 0;
                    }
                }
            }

            else if (SummShields(damageType) <= damageValue && SummShields(damageType) > 0)
            {
                float value = damageValue - SummShields(damageType);
                shields_Physic.Clear();
                return value;
            }

            return damageValue; // ���� ���� <= 0
        }

        else if (damageType == DamageType.Magical)
        {
            if (SummShields(damageType) > damageValue)
            {
                for (int i = shields_Magic.Count - 1; i >= 0; i--)
                {
                    Shielding shield = shields_Magic[i];
                    if (damageValue >= shield.shieldAmount)
                    {
                        damageValue -= shield.shieldAmount;
                        shield.shieldAmount = 0;
                        shields_Magic.Remove(shield);
                    }
                    else
                    {
                        shield.shieldAmount -= damageValue;
                        return 0;
                    }
                }
            }

            else if (SummShields(damageType) <= damageValue && SummShields(damageType) > 0)
            {
                float value = damageValue - SummShields(damageType);
                shields_Magic.Clear();
                return value;
            }

            return damageValue; // ���� ���� <= 0
        }
        return damageValue; // �� ������� ��� �����
    }
    public void TakeDamage(float damageValue, DamageType damageType)
    {        
        DisplayTakenDamage(damageValue, damageType);
        
        damageValue = CalculateDamageForShields(damageValue, damageType);
        sumDamageTaken += damageValue;

        //HandleAbsorptionOrRepeat(ref damageValue);

        if (damageValue > 0)
        {
            
            _currentHealth -= damageValue;
            if (_currentHealth <= 0)
            {
                _currentHealth = 0;
                Die();
            }
            
            UpdateHealthBar();
        }
    }

    [ContextMenu ("Add Magic Shield")] //��� ����� � ����������
    private void AddShields()
    {
        DamageType dmgtype = DamageType.Magical;
        Shielding shield = new Shielding(this, 50, dmgtype);

    }

    [ContextMenu("Add Physic Shield")] //��� ����� � ����������
    private void AddPhysShields()
    {
        DamageType dmgtype = DamageType.Physical;
        Shielding shield = new Shielding(this, 50, dmgtype);

    }

    [ContextMenu("Add Temporary Shield")] //��� ����� � ����������
    private void AddtemporaryShield()
    {
        DamageType dmgtype = DamageType.Physical;

        StartCoroutine(CoroutineAddShield(50, dmgtype, 5f));

    }
    
    public void AddShieldBehavior(Shielding shielding, DamageType damageType) // ���������� � ������������ ����� �����
    {
        if(damageType == DamageType.Physical)
        {
            shields_Physic.Add(shielding);
        }
        else if (damageType == DamageType.Magical)
        {
            shields_Magic.Add(shielding);
        }
    }

    public void AddShield(float shieldValue, DamageType damageType) // ������������ � ������������
    {
        Shielding shield = new Shielding(this, shieldValue, damageType);
    }

    public void AddShield(float shieldValue, DamageType damageType, float durationTime) // ���������� ��� ��������� �����
    {
        StartCoroutine(CoroutineAddShield(shieldValue, damageType, durationTime));
    }

    public void MakePhisicDamage(float damageValue, GameObject target)
    {
        StackTrace stackTrace = new StackTrace();
        StackFrame callerFrame = stackTrace.GetFrame(1);

        DamageInfo damageInfo;
        damageInfo.CallerType = callerFrame.GetMethod().DeclaringType;
        damageInfo.OriginalDamage = damageValue;

        damageInfo.ModifiedDamage = damageInfo.OriginalDamage;

        MakePhisicDamageEvent?.Invoke(damageInfo);

        float modifiedDamage = damageInfo.ModifiedDamage;

        //target.GetComponent<HealthComponent>().TakePhisicDamage(modifiedDamage);
    }

    //public void MakeMagicDamage(float damageValue, GameObject target)
    //{
    //    StackTrace stackTrace = new StackTrace();
    //    StackFrame callerFrame = stackTrace.GetFrame(1);

    //    DamageInfo damageInfo;
    //    damageInfo.CallerType = callerFrame.GetMethod().DeclaringType;
    //    damageInfo.OriginalDamage = damageValue;

    //    damageInfo.ModifiedDamage = damageInfo.OriginalDamage;

    //    MakeMagicDamageEvent?.Invoke(damageInfo);

    //    float modifiedDamage = damageInfo.ModifiedDamage;

    //    //target.GetComponent<HealthComponent>().TakeMagicDamage(modifiedDamage);
    //}

    public void AddHeal(float healValue)
    {
        HealInfo healthInfo;

        healthInfo.OriginalHeal = healValue;
        healthInfo.ModifiedHeal = healthInfo.OriginalHeal;
        if (AddHealth != null)
        {
            healthInfo = AddHealth(healthInfo);
        }

        _currentHealth += healthInfo.ModifiedHeal;
        if (_currentHealth >= _maxHealth)
        {
            _currentHealth = _maxHealth;
        } 
        ShowDamagePrefab(healthInfo.ModifiedHeal, new Color(0, 0.8f, 0, 1), new Color(0, 0.8f, 0, 0.5f)); 
        UpdateHealthBar();
    }

    public void RegenHP(float healValue) // ��� ������, ��� �� ����� AddHeal, �� ��� ������� ��������
    {
        HealInfo healthInfo;
        healthInfo.OriginalHeal = healValue;

        healthInfo.ModifiedHeal = healthInfo.OriginalHeal;
        if (AddHealth != null)
        {
            healthInfo = AddHealth(healthInfo);
        }

        float modifiedHeal = healthInfo.ModifiedHeal;

        _currentHealth += modifiedHeal;
        if (_currentHealth >= _maxHealth)
        {
            _currentHealth = _maxHealth;
        }
        UpdateHealthBar();
    }

    private void HandleAbsorptionOrRepeat(ref float modifiedValue)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);

            DamageAbsorption damageAbsorption = child.GetComponent<DamageAbsorption>();
            if (damageAbsorption != null)
            {
                damageAbsorption.Absorption(ref modifiedValue);
            }

            RepeatedDamage repeatedDamage = child.GetComponent<RepeatedDamage>();
            if (repeatedDamage != null && !repeatedDamage.IsRepeat)
            {
                repeatedDamage.RepeatDamage(ref modifiedValue);
            }
        }
        PsionicaMelee psionicaMelee = GetComponent<PsionicaMelee>();
        if (psionicaMelee != null)
        {
            psionicaMelee.PsionicaAbsorption(ref modifiedValue);

        }
    }
    private void DisplayTakenDamage(float damageValue, DamageType damageType)
    {
        if (damageType == DamageType.Physical)
        {
            ShowDamagePrefab(-damageValue, new Color(1, 0, 0, 1), new Color(1, 0, 0, 0.5f));
        }
        if (damageType == DamageType.Magical)
        {
            ShowDamagePrefab(-damageValue, new Color(140, 0, 255, 1), new Color(140, 0, 255, 0.5f));
        }
    }
    private void ShowDamagePrefab(float value, Color startColor, Color endColor)
    {
        GetComponent<UIPlayerComponents>().ShowPopupValue(value,startColor,endColor);
    }

    private void ShowDamagePrefab(string text, Color startColor, Color endColor) //������������ ��� �������
    {
        GetComponent<UIPlayerComponents>().ShowPopupText(text,startColor,endColor);
    }

    public void UpdateHealthBar()
    {
        healthBar.UpdateValue(_currentHealth,_maxHealth);
    }

    public void NetworkUpdateHealthBar(float oldValue, float newValue)
    {
        UpdateHealthBar();
    }

    private void Die()
    {
       
    }

    private IEnumerator CoroutineRegenirateHP()
    {
        while (true)
        {
            yield return new WaitForSeconds(_hpRegenerationDelay);
            if(_currentHealth < _maxHealth)
            {
                this.RegenHP(_hpRegenerationValue + _hpRegenerationValue * _boostRegen + +_hpRegenerationValue * _boostRegen2);
            }
        }
    }

    private IEnumerator CoroutineAddShield(float shieldValue, DamageType damageType,float shieldsDuration) 
    {
        Shielding shield = new TemporaryShielding(this, shieldValue, damageType, shieldsDuration);

        yield return new WaitForSeconds(shieldsDuration);

        if(damageType == DamageType.Physical)
        {
            if (shield != null)
            {
                shield.shieldAmount = 0;
                shields_Physic.Remove(shield);
                UnityEngine.Debug.LogWarning("Im expired");
            }
        }

        if (damageType == DamageType.Magical)
        {
            if (shield != null)
            {
                shield.shieldAmount = 0;
                shields_Magic.Remove(shield);
                UnityEngine.Debug.LogWarning("Im expired");
            }
        }
    }

    //��, ��� ����, ��������� �����.... ���� ��� �������� ��� ������������� �����
    public void SetBoostRegen(float boostRegen) 
    {
        _boostRegen = boostRegen;
	}
	public void SetBoostRegen2(float boostRegen)
	{
		_boostRegen2 = boostRegen;
	}
    public void SetInvincible(bool invincible)
    {
        _invinsible = invincible;
    }

    public void SetEvadeMagic(float value)
    {
        _evadeMagDamage =+ value;
    }    

    public float GetEvadeMagic()
    {
        return _evadeMagDamage;
    }
}
