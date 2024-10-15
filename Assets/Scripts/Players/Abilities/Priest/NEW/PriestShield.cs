using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class PriestShield : Skill
{
    //---------------- LightSettings
    [Header("Shield (Light Mode) Settings")]
    [SerializeField] private float lightShieldDuration = 18f;
    [SerializeField] private float tiredSoulDuration = 12f;
    [SerializeField] private float selfCastTime = 0.6f;
    [SerializeField] private float allyCastTime = 1.2f;
    [SerializeField] private float absorbAmount = 20f;
    [SerializeField] private List<SkillEnergyCost> manaCostLight;
    [SerializeField] private float cooldownLight = 4f;

    //---------------- DarkSettings
    [Header("Shield (Dark Mode) Settings")]
    [SerializeField] private float darkShieldDuration = 12f;
    [SerializeField] private float maxDamagePerTick = 20f;
    [SerializeField] private List<SkillEnergyCost> manaCostDark;
    [SerializeField] private float cooldownDark = 4f;
    [SerializeField] private float darkCastTime = 1.2f;

    //---------------- Talent 1 (Physical Damage Boost)
    private bool _talentPhysicalShieldBoostActive = false;
    private float _physicalDamageAccumulated = 0;
    private const float MaxPhysicalBoostPercentage = 0.5f;
    private const float PhysicalBoostPerDamageUnit = 0.1f;

    //---------------- Talent 2 (Discipline Shield Boost)
    private bool _disciplineShieldBoostActive = false;
    private int _disciplineStacks = 0;
    private const int MaxDisciplineStacks = 3;
    private const float DisciplineBoostPercentage = 0.1f;

    //---------------- Talent 3 (Dark Magic Damage Boost)
    private bool _talentDarkMagicBoostActive = false;
    private const float MaxDarkMagicBoostPercentage = 0.5f;
    private const float DarkMagicBoostPerUnit = 1f;
    private const float DarkMagicUnit = 10f;

    //---------------- Talent 4 (Healing Boost)
    private bool _talentHealingBoostActive = false;
    private const float MaxHealingBoostPercentage = 0.5f;
    private const float HealingBoostPerUnit = 1f;
    private const float HealingUnit = 10f;

    private float _absorbBonus = 0;
    private float _damagePerTickBonus = 0;
    private Character _target;
    private float _nextAvailableTime;
    public bool isLightMode = true;

    protected override bool IsCanCast => IsCanCastCheck();

    private bool IsCanCastCheck()
    {
        if (_target == null || Time.time < _nextAvailableTime) return false;
        return Vector3.Distance(transform.position, _target.transform.position) <= Radius;
    }

    public event Action OnModeChange;

    private void OnEnable()
    {
        OnModeChange += HandleModeChange;
        UpdateMode();

        Hero.Health.DamageTaken += HandleDamageTaken;

        foreach (var skill in Hero.Abilities.Abilities.Where(skill => skill.AbilityForm == AbilityForm.Magic))
        {
            skill.CastEnded += AddDisciplineStack;
        }
    }

    private void OnDisable()
    {
        OnModeChange -= HandleModeChange;
        Hero.Health.DamageTaken -= HandleDamageTaken;

        foreach (var skill in Hero.Abilities.Abilities.Where(skill => skill.AbilityForm == AbilityForm.Magic))
        {
            skill.CastEnded -= AddDisciplineStack;
        }
    }

    public void SwitchMode()
    {
        isLightMode = !isLightMode;
        OnModeChange?.Invoke();
    }

    private void HandleModeChange()
    {
        UpdateMode();
    }

    private void UpdateMode()
    {
        CastDeley = isLightMode ? allyCastTime : darkCastTime;
        CooldownTime = isLightMode ? cooldownLight : cooldownDark;
        School = isLightMode ? Schools.Light : Schools.Dark;
        TargetsLayers = isLightMode ? LayerMask.GetMask("Allies") : LayerMask.GetMask("Enemy");
    }

    //---------------- Talent 1 Logic: Physical Shield Boost ----------------
    public void EnableTalentPhysicalShieldBoost(bool value)
    {
        _talentPhysicalShieldBoostActive = value;
        if (!value)
        {
            _physicalDamageAccumulated = 0;
        }
    }

    private void HandleDamageTaken(float damage, DamageType damageType, Skill skill)
    {
        if (!_talentPhysicalShieldBoostActive || damageType != DamageType.Physical) return;

        _physicalDamageAccumulated += damage;
        UpdatePhysicalDamageAccumulation();
    }

    private void UpdatePhysicalDamageAccumulation()
    {
        var amountBonus = Mathf.Min(_physicalDamageAccumulated * PhysicalBoostPerDamageUnit, absorbAmount * MaxPhysicalBoostPercentage);
        _absorbBonus = amountBonus;
    }

    //---------------- Talent 2 Logic: Discipline Shield Boost ----------------
    public void EnableDisciplineShieldBoost(bool value)
    {
        _disciplineShieldBoostActive = value;
        if (!value)
        {
            _disciplineStacks = 0;
        }
    }

    private void AddDisciplineStack()
    {
        if (_disciplineShieldBoostActive && _disciplineStacks < MaxDisciplineStacks)
        {
            _disciplineStacks++;
            Debug.Log($"Discipline stack added. Current stacks: {_disciplineStacks}");
        }
    }

    private void ApplyDisciplineBoost()
    {
        if (_disciplineShieldBoostActive && _disciplineStacks > 0)
        {
            var boostPercentage = DisciplineBoostPercentage * _disciplineStacks;
            absorbAmount *= (1 + boostPercentage);
            Debug.Log($"Applied discipline boost. Boost percentage: {boostPercentage * 100}%");
            _disciplineStacks = 0;
        }
    }

    //---------------- Talent 3 Logic: Dark Magic Damage Boost ----------------
    public void EnableDarkMagicBoost(bool value)
    {
        _talentDarkMagicBoostActive = value;
        
        if (value) return;
        
        _damagePerTickBonus = 0;
        Hero.DamageTracker.RemoveOldLocalEntries();
    }

    private void ApplyDarkMagicBoost()
    {
        if (!_talentDarkMagicBoostActive) return;

        float darkMagicDamage = Hero.DamageTracker.GetLocalDamageInTime(Schools.Dark, 5f);
        float boostUnits = Mathf.Floor(darkMagicDamage / DarkMagicUnit);
        float boostAmount = Mathf.Min(boostUnits * DarkMagicBoostPerUnit, absorbAmount * MaxDarkMagicBoostPercentage);

        _absorbBonus += boostAmount;
        Debug.Log($"Dark Magic boost applied. Damage: {darkMagicDamage}, Boost: {boostAmount}");
    }

    //---------------- Talent 4 Logic: Healing Boost ----------------
    public void EnableHealingBoost(bool value)
    {
        _talentHealingBoostActive = value;
        if(Hero == null || Hero.DamageTracker == null) return;
        Hero.DamageTracker.RemoveOldLocalEntries();
    }

    private void ApplyHealingBoost()
    {
        if (!_talentHealingBoostActive) return;

        float healingAmount = Hero.DamageTracker.GetLocalHealInTime(5f);
        float boostUnits = Mathf.Floor(healingAmount / HealingUnit);
        float boostAmount = Mathf.Min(boostUnits * HealingBoostPerUnit, absorbAmount * MaxHealingBoostPercentage);

        _absorbBonus += boostAmount;
        Debug.Log($"Healing boost applied. Healing: {healingAmount}, Boost: {boostAmount}");
    }

    protected override IEnumerator PrepareJob()
    {
        while (_target == null)
        {
            if (Input.GetMouseButton(0))
            {
                _target = GetRaycastTarget(true);

                if (_target == transform.GetComponentInParent<Character>())
                {
                    CastDeley = selfCastTime;
                }
            }
            yield return null;
        }
    }

    protected override IEnumerator CastJob()
    {
        if (_target == null || !IsCanCast) yield break;

        _nextAvailableTime = Time.time + CooldownTime;

        if (isLightMode)
        {
            HandleLightShield();
        }
        else
        {
            HandleDarkShield();
        }

        yield return null;
    }

    private void HandleLightShield()
    {
        if (_target == null) return;

        if (!TryPayCost(manaCostLight)) return;
        
        ApplyDisciplineBoost();
        ApplyDarkMagicBoost();
        ApplyHealingBoost();

        CmdAddDebaff(States.LightShield, States.TiredSoul, lightShieldDuration, tiredSoulDuration, absorbAmount + _absorbBonus, _target.gameObject, name);
        Debug.Log("Light Shield applied to " + _target.name);
    }

    private void HandleDarkShield()
    {
        if (_target == null) return;

        if (!TryPayCost(manaCostDark)) return;

        CmdAddBaff(States.DarkShield, darkShieldDuration, maxDamagePerTick + _damagePerTickBonus, _target.gameObject, name);
        Debug.Log("Dark Shield applied to " + _target.name);
    }

    [Command]
    private void CmdAddDebaff(States lightState, States tiredState, float duration, float tiredDuration, float damageToExit, GameObject target, string skillName)
    {
        var characterState = target.GetComponent<CharacterState>();
        if (characterState.CheckForState(States.TiredSoul))
        {
            Debug.Log("Cannot apply Light Shield, target is tired.");
            return;
        }

        characterState.AddState(lightState, duration, damageToExit, target, skillName);
        characterState.AddState(tiredState, tiredDuration, damageToExit, target, skillName);
    }

    [Command]
    private void CmdAddBaff(States darkState, float duration, float damagePerTick, GameObject target, string skillName)
    {
        var characterState = target.GetComponent<CharacterState>();
        characterState.AddState(darkState, duration, damagePerTick, target, skillName);
    }
    
    protected override void ClearData()
    {
        _target = null;
        _damagePerTickBonus = 0;
        _absorbBonus = 0;
    }
}