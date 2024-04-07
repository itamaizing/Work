using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using UnityEngine;

public class HealthPlayer : MonoBehaviour
{
    [SerializeField][Range(0, 100)] private float _hpRegenerationValue = 10;
    [SerializeField][Range(0, 100)] private float _hpRegenerationDelay = 3;
    private WaitForSeconds _waitForRegenHp;

    [Header("Def Stats")]
    [SerializeField] private float defPh = 10f;
    [SerializeField] private float defMag = 10f;
    [SerializeField] private int evamel = 10;
    [SerializeField] private int evaran = 10;

    [Header("Shields")]
    public List<Shielding> shields_Physic = new List<Shielding>();
    public List<Shielding> shields_Magic = new List<Shielding>();
    [Space]

    public float Health;
    public float MaxHealth;
    public GameObject HealthBar;
    public TextMeshPro HealthBarText;
    public Transform DamageSpawn;
    public TextMeshPro PrefabText;

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

    private void Start()
    {
        UpdateHealthBar();
        _waitForRegenHp = new WaitForSeconds(_hpRegenerationDelay);
        StartCoroutine(CoroutineRegenirateHP());
    }

    public void TakePhisicDamage(float damageValue)
    {

        HandleAbsorptionOrRepeat(ref damageValue);
        if (damageValue > 0)
        {
            StackTrace stackTrace = new StackTrace();
            StackFrame callerFrame = stackTrace.GetFrame(1);

            DamageInfo damageInfo;
            damageInfo.CallerType = callerFrame.GetMethod().DeclaringType;

            damageInfo.OriginalDamage = damageValue;

            damageInfo.ModifiedDamage = damageInfo.OriginalDamage;

            OnTakePhisicDamage?.Invoke(damageInfo);

            float modifiedDamage = damageInfo.ModifiedDamage;
            Health -= modifiedDamage;
            if (Health <= 0)
            {
                Health = 0;
                Die();
            }
            ShowDamagePrefab(-modifiedDamage, new Color(1, 0, 0, 1), new Color(1, 0, 0, 0.5f));
            UpdateHealthBar();
            UpdateHealthBarText();
        }
    }

    private float CalculateDamageWithStats(float damageValue, DamageType damageType, AttackRangeType attackRangeType)
    {
        if (damageType == DamageType.Magical)
        {
            return damageValue - (damageValue * defMag / 100);
        }

        else if (damageType == DamageType.Physical)
        {
            switch (attackRangeType)
            {
                case AttackRangeType.MeleeAttack:
                    if (UnityEngine.Random.Range(0, 100) <= evamel)
                    {
                        ShowDamagePrefab(new Color(120, 120, 120, 1), new Color(120, 120, 120, 0.5f), "miss");
                        return 0;
                    }
                    return damageValue - (damageValue * defPh / 100);

                case AttackRangeType.RangeAttack:
                    if (UnityEngine.Random.Range(0, 100) <= evaran)
                    {
                        ShowDamagePrefab(new Color(120, 120, 120, 1), new Color(120, 120, 120, 0.5f), "miss");
                        return 0;
                    }
                    return damageValue - (damageValue * defPh / 100);

                case AttackRangeType.Inner:
                    return damageValue;

                default:
                    return 0; // не указали AttackRangeType

            }
        }
        return 0; // не указали DamageType
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

            return damageValue; // если щиты <= 0
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

            return damageValue; // если щиты <= 0
        }
        return damageValue; // не указали тип урона
    }
    public void TakeDamage(float damageValue, DamageType damageType, AttackRangeType attackRangeType)
    {
        UnityEngine.Debug.LogWarning($"baseDamage: {damageValue}");

        damageValue = CalculateDamageWithStats(damageValue, damageType, attackRangeType);

        DisplayTakenDamage(damageValue, damageType);
        
        damageValue = CalculateDamageForShields(damageValue, damageType);

        HandleAbsorptionOrRepeat(ref damageValue);

        if (damageValue > 0)
        {
            
            Health -= damageValue;
            if (Health <= 0)
            {
                Health = 0;
                Die();
            }
            
            //ShowDamagePrefab(-modifiedDamage, new Color(1, 0, 0, 1), new Color(1, 0, 0, 0.5f));
            UpdateHealthBar();
            UpdateHealthBarText();
        }
    }

    public void TakeMagicDamage(float damageValue)
    {
        HandleAbsorptionOrRepeat(ref damageValue);
        if (damageValue > 0)
        {
            StackTrace stackTrace = new StackTrace();
            StackFrame callerFrame = stackTrace.GetFrame(1);

            DamageInfo damageInfo;
            damageInfo.CallerType = callerFrame.GetMethod().DeclaringType;
            damageInfo.OriginalDamage = damageValue;

            damageInfo.ModifiedDamage = damageInfo.OriginalDamage;

            OnTakeMagicDamage?.Invoke(damageInfo);

            float modifiedDamage = damageInfo.ModifiedDamage;
            Health -= modifiedDamage;
            if (Health <= 0)
            {
                Health = 0;
                Die();
            }
            ShowDamagePrefab(-modifiedDamage, new Color(1, 0, 0, 1), new Color(1, 0, 0, 0.5f));
            UpdateHealthBar();
            UpdateHealthBarText();
        }
    }

    [ContextMenu ("Add Magic Shield")]
    public void AddShields()
    {
        DamageType dmgtype = DamageType.Magical;
        Shielding shield = new Shielding(this, 50, dmgtype);

    }

    [ContextMenu("Add Physic Shield")]
    public void AddPhysShields()
    {
        DamageType dmgtype = DamageType.Physical;
        Shielding shield = new Shielding(this, 50, dmgtype);

    }

    [ContextMenu("Add Temporary Shield")]
    public void AddtemporaryShield()
    {
        DamageType dmgtype = DamageType.Physical;

        StartCoroutine(CoroutineAddShield(50, dmgtype, 5f));

    }
    
    public void AddShieldBehavior(Shielding shielding, DamageType damageType) // вызывается в конструкторе самих щитов
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

    public void AddShield(float shieldValue, DamageType damageType)
    {
        Shielding shield = new Shielding(this, shieldValue, damageType);
    }

    public void AddShield(float shieldValue, DamageType damageType, float durationTime) // перегрузка для временных щитов
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

        target.GetComponent<HealthPlayer>().TakePhisicDamage(modifiedDamage);
    }

    public void MakeMagicDamage(float damageValue, GameObject target)
    {
        StackTrace stackTrace = new StackTrace();
        StackFrame callerFrame = stackTrace.GetFrame(1);

        DamageInfo damageInfo;
        damageInfo.CallerType = callerFrame.GetMethod().DeclaringType;
        damageInfo.OriginalDamage = damageValue;

        damageInfo.ModifiedDamage = damageInfo.OriginalDamage;

        MakeMagicDamageEvent?.Invoke(damageInfo);

        float modifiedDamage = damageInfo.ModifiedDamage;

        target.GetComponent<HealthPlayer>().TakeMagicDamage(modifiedDamage);
    }

    public void AddHeal(float healValue)
    {
        HealInfo healthInfo;
        healthInfo.OriginalHeal = healValue;

        healthInfo.ModifiedHeal = healthInfo.OriginalHeal;
        if (AddHealth != null)
        {
            healthInfo = AddHealth(healthInfo);
        }

        float modifiedHeal = healthInfo.ModifiedHeal;

        Health += modifiedHeal;
        if (Health >= MaxHealth)
        {
            Health = MaxHealth;
        }
        ShowDamagePrefab(modifiedHeal, new Color(0, 0.8f, 0, 1), new Color(0, 0.8f, 0, 0.5f));
        UpdateHealthBar();
        UpdateHealthBarText();

    }

    public void RegenHP(float healValue) // для регена, тот же самый AddHeal, но без префаба значения
    {
        HealInfo healthInfo;
        healthInfo.OriginalHeal = healValue;

        healthInfo.ModifiedHeal = healthInfo.OriginalHeal;
        if (AddHealth != null)
        {
            healthInfo = AddHealth(healthInfo);
        }

        float modifiedHeal = healthInfo.ModifiedHeal;

        Health += modifiedHeal;
        if (Health >= MaxHealth)
        {
            Health = MaxHealth;
        }
        UpdateHealthBar();
        UpdateHealthBarText();

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
        if(value > 0 && value < 1)
        {
            value = 1;
        }
        value = (int)value;
        PrefabText.text = (value > 0 ? "+" : "") + value.ToString();
        PrefabText.GetComponent<DamagePrefab>().StartColor = startColor;
        PrefabText.GetComponent<DamagePrefab>().EndColor = endColor;
        TextMeshPro newPrefab = Instantiate(PrefabText, DamageSpawn.position, Quaternion.identity);
        newPrefab.transform.SetParent(transform);
    }

    private void ShowDamagePrefab(Color startColor, Color endColor, string text) //используется при промахе
    {
        PrefabText.text = text;
        PrefabText.GetComponent<DamagePrefab>().StartColor = startColor;
        PrefabText.GetComponent<DamagePrefab>().EndColor = endColor;
        TextMeshPro newPrefab = Instantiate(PrefabText, DamageSpawn.position, Quaternion.identity);
        newPrefab.transform.SetParent(transform);
    }

    public void UpdateHealthBar()
    {
        float newScaleX = Health / MaxHealth;
        HealthBar.transform.localScale = new Vector3(newScaleX, 1.0f, 1.0f);
    }
    private void UpdateHealthBarText()
    {
        float healthValue = (int)Health;
        HealthBarText.text = healthValue.ToString();
    }
    private void Die()
    {
       
    }

    private IEnumerator CoroutineRegenirateHP()
    {
        while (true)
        {
            yield return _waitForRegenHp;
            this.RegenHP(_hpRegenerationValue);
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
}
