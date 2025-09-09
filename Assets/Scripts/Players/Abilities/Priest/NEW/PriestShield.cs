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

    [SerializeField] private AudioClip audioClip;

    private AudioSource _audioSource;

    //DisciplineTalent_4
    private bool _talentPhysicalShieldBoostActive = false;
    private float _physicalDamageAccumulated = 0;
    private const float MaxPhysicalBoostPercentage = 0.5f;
    private const float PhysicalBoostPerDamageUnit = 0.1f;
    private float _physDamageAccumulator = 0f;
    private float _lastPhysDamageTime = -999f;
    private const float PhysBoostTimeWindow = 5f;

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
    
    //---------------- Talent 5 (Tired Soul Evade)
    private bool _talentTiredSoulActive = false;
    private const float TiredSoulEffectPercentage = 0.5f;

    private float _absorbBonus = 0;
    private float _damagePerTickBonus = 0;
    private Character _target;
    private float _nextAvailableTime;
    public bool isLightMode = true;

    protected override bool IsCanCast => IsCanCastCheck();

    protected override int AnimTriggerCastDelay => Animator.StringToHash("PriestShield");
    protected override int AnimTriggerCast => 0;

    private bool IsCanCastCheck()
    {
        if (_target == null || Time.time < _nextAvailableTime) return false;
        return Vector3.Distance(transform.position, _target.transform.position) <= Radius;
    }

    public event Action OnModeChange;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        OnModeChange += HandleModeChange;
        UpdateMode();

        Hero.Health.DamageTaken += HandleDamageTaken;

        foreach (var skill in Hero.Abilities.Abilities.Where(skill => skill.School == Schools.Discipline))
        {
            skill.CastEnded += AddDisciplineStack;
        }
    }

    private void OnDisable()
    {
        OnModeChange -= HandleModeChange;
        Hero.Health.DamageTaken -= HandleDamageTaken;

        foreach (var skill in Hero.Abilities.Abilities.Where(skill => skill.School == Schools.Discipline))
        {
            skill.CastEnded -= AddDisciplineStack;
        }
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        _target = (Character)targetInfo.Targets[0];
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

    //DisciplineTalent_4
    public void EnableTalentPhysicalShieldBoost(bool value)
    {
        _talentPhysicalShieldBoostActive = value;
        if (!value)
        {
            _physicalDamageAccumulated = 0;
        }
    }

    private void HandleDamageTaken(Damage damage, Skill skill)
    {
        if (!_talentPhysicalShieldBoostActive || damage.Type != DamageType.Physical) return;

        _physDamageAccumulator += damage.Value;
        _lastPhysDamageTime = Time.time;
    }

    private void UpdatePhysicalDamageAccumulation()
    {
        var amountBonus = Mathf.Min(_physicalDamageAccumulated * PhysicalBoostPerDamageUnit, absorbAmount * MaxPhysicalBoostPercentage);
        _absorbBonus += amountBonus;
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
            _absorbBonus += absorbAmount * boostPercentage;
            Debug.Log($"Applied discipline boost. Boost percentage: {boostPercentage * 100}%");
            _disciplineStacks = 0;
        }
    }

    //---------------- Talent 3 Logic: Dark Magic Damage Boost ----------------
    public void EnableDarkMagicBoost(bool value)
    {
        _talentDarkMagicBoostActive = value;
        
        if (_talentDarkMagicBoostActive) return;
        
        _damagePerTickBonus = 0;
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
    
    public void EnableTiredSoulEvade(bool value)
    {
        _talentTiredSoulActive = value;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (_target == null)
        {
            if (Input.GetMouseButton(0))
            {
                _target = GetRaycastTarget(true);

                if (_target == transform.GetComponentInParent<Character>())
                {
                    _absorbBonus = 0;
                    CastDeley = selfCastTime;
                }
            }
            yield return null;
        }

        TargetInfo targetInfo = new();
        targetInfo.Targets.Add(_target);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (_target == null || !IsCanCast) yield break;
        Cast();

        yield return null;
    }

    private void Cast()
    {
        _nextAvailableTime = Time.time + CooldownTime;

        CmdPlayShootSound();

        if (isLightMode)
        {
            HandleLightShield();
        }
        else
        {
            HandleDarkShield();
        }
    }

    private void HandleLightShield()
    {
        if (_target == null) return;

        if (!TryPayCost(manaCostLight)) return;
        
        ApplyDisciplineBoost(); // todo ??
        
        ApplyDarkMagicBoost();
        ApplyHealingBoost();

        float physicalBoost = 0;

        if (_talentPhysicalShieldBoostActive && Time.time - _lastPhysDamageTime <= PhysBoostTimeWindow)
        {
            float bonusUnits = Mathf.Floor(_physDamageAccumulator / 10f);
            physicalBoost = Mathf.Min(bonusUnits, absorbAmount * MaxPhysicalBoostPercentage);
        }

        _absorbBonus += physicalBoost;
        _physDamageAccumulator = 0;
        _lastPhysDamageTime = -999f;

        var characterState = _target.GetComponent<CharacterState>();
        var duration = _talentTiredSoulActive && characterState.CheckForState(States.TiredSoul) ? lightShieldDuration * TiredSoulEffectPercentage : lightShieldDuration;
        var absorbDamage = _talentTiredSoulActive && characterState.CheckForState(States.TiredSoul)
            ? (absorbAmount + _absorbBonus) * TiredSoulEffectPercentage
            : absorbAmount + _absorbBonus;
        
        Debug.Log(_absorbBonus);

        CmdAddDebaff(States.LightShield, States.TiredSoul, duration, tiredSoulDuration, absorbDamage, _target.gameObject, Name);
        Debug.Log("Light Shield applied to " + _target.name);
    }

    private void HandleDarkShield()
    {
        if (_target == null) return;

        if (!TryPayCost(manaCostDark)) return;

        CmdAddBaff(States.DarkShield, darkShieldDuration, maxDamagePerTick + _damagePerTickBonus, _target.gameObject, Name);
        Debug.Log("Dark Shield applied to " + _target.name);
    }

    public void PriestShieldCast()
    {
        AnimStartCastCoroutine();
    }

    public void PriestShieldEnd()
    {
        AnimCastEnded();
    }

    [Command]
    private void CmdAddDebaff(States lightState, States tiredState, float duration, float tiredDuration,
        float damageToExit, GameObject target, string skillName)
    {
        var characterState = target.GetComponent<CharacterState>();

        if (!_talentTiredSoulActive)
        {
            if (characterState.CheckForState(tiredState))
            {
                Debug.Log("Cannot apply Light Shield, target already has TiredSoul and talent is inactive.");
                return;
            }
            
            Debug.Log("Talent is inactive, applying LightShield and TiredSoul.");
            characterState.AddState(lightState, duration, damageToExit, target, skillName);
            characterState.AddState(tiredState, tiredDuration, damageToExit, target, skillName);
        }
        else
        {
            if (characterState.CheckForState(tiredState))
            {
                int tiredSoulStacks = characterState.CheckStateStacks(tiredState);
                
                Debug.Log($"Talent is active. TiredSoul stacks: {tiredSoulStacks}");
                
                if (tiredSoulStacks >= 2)
                {
                    Debug.Log("TiredSoul has 2 or more stacks, exiting without applying LightShield.");
                    return;
                }
                
                Debug.Log("TiredSoul has less than 2 stacks, applying LightShield and TiredSoul.");
                characterState.AddState(lightState, duration, damageToExit, target, skillName);
                characterState.AddState(tiredState, tiredDuration, damageToExit, target, skillName);
            }
            else
            {
                Debug.Log("Talent is active, but target does not have TiredSoul. Applying LightShield and TiredSoul.");
                characterState.AddState(lightState, duration, damageToExit, target, skillName);
                characterState.AddState(tiredState, tiredDuration, damageToExit, target, skillName);
            }
        }
    }

    [Command]
    private void CmdAddBaff(States darkState, float duration, float damagePerTick, GameObject target, string skillName)
    {
        var characterState = target.GetComponent<CharacterState>();
        characterState.AddState(darkState, duration, damagePerTick, target, skillName);
    }


    [Command]
    private void CmdPlayShootSound()
    {
        RpcPlayShotSound();
    }

    [ClientRpc]
    private void RpcPlayShotSound()
    {
        if (_audioSource != null && audioClip != null) _audioSource.PlayOneShot(audioClip);
    }

    protected override void ClearData()
    {
        _target = null;
        _damagePerTickBonus = 0;
        _absorbBonus = 0;
    }
}