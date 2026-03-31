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
    [SerializeField] private float absorbAmount = 40f;

    [SerializeField] private AudioClip audioClip;

    private AudioSource _audioSource;

    //DisciplineTalent_4
    private bool _talentPhysicalShieldBoostActive = false;
    private const float MaxPhysicalBoostPercentage = 0.5f;
    private const float PhysicalBoostPerDamageUnit = 1f;
    private float _physDamageAccumulator = 0f;
    private float _lastPhysDamageTime = -999f;
    private const float PhysicUnit = 10f;
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
    private float _accumulatedDarkDamage = 0f;
    private float _lastDarkDamageTime = -999f;
    private const float DarkDamageResetTime = 5f;

    //---------------- Talent 4 (Healing Boost)
    private bool _talentHealingBoostActive = false;
    private const float MaxHealingBoostPercentage = 0.5f;
    private const float HealingBoostPerUnit = 1f;
    private float _healingAccumulator = 0f;
    private float _lastHealingTime = -999f;
    private const float HealingUnit = 10f;
    
    //---------------- Talent 5 (Tired Soul Evade)
    private bool _talentTiredSoulActive = false;
    private const float TiredSoulEffectPercentage = 0.5f;
    
    /*//................ Talent 9 (Reflection shield)
    private bool _shieldAttackTalentActive = false;
    public void EnableShieldAttackTalent(bool value)
    {
        _shieldAttackTalentActive = value;
    }*/

    private float _absorbBonus = 0;
    private float _damagePerTickBonus = 0;
    //private IDamageable _target;
    //private Character _targetCharacter;
    private float _nextAvailableTime;
    public bool isLightMode = true;
    private float _clickRadius = 0.5f;

    protected override int AnimTriggerCastDelay => Animator.StringToHash("PriestShield");
    protected override int AnimTriggerCast => 0;
    
    private bool IsAllyTarget(Character target) => target != null && target.gameObject.layer == LayerMask.NameToLayer("Allies");
    private bool IsEnemyTarget(Character target) => target != null && target.gameObject.layer == LayerMask.NameToLayer("Enemy");
    
    public override bool IsPayCostStartCooldown => false;

    public event Action OnModeChange;
    
    private readonly Dictionary<PriestBoosterType, SkillTalentHandler> _boosters = new();

    #region Boosters

    #region Enums

    public enum PriestBoosterType
    {
        None = 0,
        SpiritShieldReflection,
        LightShieldManaRestore
    }

    #endregion

    
    #region RelfectionShield

    private SpiritShieldReflectionBooster _spiritShieldReflectionBooster;
    public SpiritShieldReflectionBooster SpiritShieldReflectionBooster => _spiritShieldReflectionBooster;

    #endregion

    #region ShieldManaRestore

    private LightShieldManaRestoreBooster _lightShieldManaBooster;
    public LightShieldManaRestoreBooster LightShieldManaRestoreBooster => _lightShieldManaBooster;

    #endregion

    #endregion
    
    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        Hero.DamageTracker.OnDamageTracked += TrackDarkDamage;
        Hero.Health.DamageTaken += TrackPhysDamage;
        Hero.DamageTracker.OnHealTracked += TrackHealDone;
        //Hero.Health.ShieldDamageTaken += OnShieldDamageTaken;

        foreach (var skill in Hero.Abilities.Abilities.Where(skill => skill.Info.School == Schools.Discipline))
            skill.CastEnded += AddDisciplineStack;
        
        _spiritShieldReflectionBooster = new SpiritShieldReflectionBooster(this);
        _lightShieldManaBooster = new LightShieldManaRestoreBooster(this);

        RegisterBooster(PriestBoosterType.SpiritShieldReflection, _spiritShieldReflectionBooster);
        RegisterBooster(PriestBoosterType.LightShieldManaRestore, _lightShieldManaBooster);
    }

    private void OnDisable()
    {
        Hero.DamageTracker.OnDamageTracked -= TrackDarkDamage;
        Hero.Health.DamageTaken -= TrackPhysDamage;
        Hero.DamageTracker.OnHealTracked -= TrackHealDone;
        //Hero.Health.ShieldDamageTaken -= OnShieldDamageTaken;

        foreach (var skill in Hero.Abilities.Abilities.Where(skill => skill.Info.School == Schools.Discipline))
            skill.CastEnded -= AddDisciplineStack;
    }

    private void RegisterBooster(PriestBoosterType type, SkillTalentHandler booster)
    {
        _boosters[type] = booster;
    }
    
    public void EnableBooster(PriestBoosterType type, bool value)
    {
        if (!isClient) return;
        CmdEnableBooster(type, value);
    }

    [Command]
    private void CmdEnableBooster(PriestBoosterType type, bool value)
    {
        if (_boosters.TryGetValue(type, out var booster))
        {
            booster.Enable(value);
        }
    }
    
    public void EnableReflectionBooster(bool value) 
        => EnableBooster(PriestBoosterType.SpiritShieldReflection, value);

    public void TryApplyTalents(Character reflector, Damage incomingDamage, Skill sourceSkill)
    {
        if (_spiritShieldReflectionBooster.TryReflectDamage(reflector, incomingDamage, sourceSkill))
        {
            bool isOnSelf = reflector == Hero;
            bool hasReversePolarity = Hero.CharacterState.CheckForState(States.ReversePolarity);

            if (isOnSelf && hasReversePolarity)
            {
                RpcReflectAoe(reflector.gameObject,incomingDamage);
            }
            else
            {
                _spiritShieldReflectionBooster.ReflectDamageToAttacker(incomingDamage, sourceSkill);
            }
            if (_lightShieldManaBooster.Enabled)
            {
                _lightShieldManaBooster.OnShieldAbsorbedDamage(reflector, incomingDamage.Value * _spiritShieldReflectionBooster.ReflectionDamagePercent);
            }
        }
    }

    [ClientRpc]
    private void RpcReflectAoe(GameObject caster, Damage damage)
    {
        _spiritShieldReflectionBooster.ReflectDamageAoE(caster.GetComponent<Character>(), damage);
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        Targeting.SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
    }

    #region Track bonus
    private void TrackDarkDamage(Damage damage, GameObject target)
    {
        if (damage.School != Schools.Dark) return;

        if (Time.time - _lastDarkDamageTime > DarkDamageResetTime)
        {
            _accumulatedDarkDamage = 0f;
        }

        _accumulatedDarkDamage += damage.Value;
        _lastDarkDamageTime = Time.time;
    }

    private void TrackPhysDamage(Damage damage, Skill skill)
    {
        if (damage.School != Schools.Physical) return;

        if (Time.time - _lastPhysDamageTime > PhysBoostTimeWindow)
        {
            _physDamageAccumulator = 0f;
        }

        _physDamageAccumulator += damage.Value;
        _lastPhysDamageTime = Time.time;
    }

    private void TrackHealDone(Heal heal)
    {
        if (heal.DamageableSkill == null) return;
        if (heal.DamageableSkill.Info.School != Schools.Light) return;

        if (Time.time - _lastHealingTime > PhysBoostTimeWindow)
        {
            _healingAccumulator = 0f;
        }

        _healingAccumulator += heal.Value;
        _lastHealingTime = Time.time;
    }
    #endregion

    private float GetAccumulated(float lastTime, float resetTime, float accumulated)
    {
        if (Time.time - lastTime > resetTime) accumulated = 0f;
        return accumulated;
    }

    //DisciplineTalent_4
    public void EnableTalentPhysicalShieldBoost(bool value)
    {
        _talentPhysicalShieldBoostActive = value;
    
        if (value) EnableSkillBoost();
        else
        {
            DisableSkillBoost();
            _physDamageAccumulator = 0;
        }
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

    //---------------- Talent 3 Logic: Dark Magic Damage Boost ----------------
    public void EnableDarkMagicBoost(bool value)
    {
        _talentDarkMagicBoostActive = value;
        
        if (_talentDarkMagicBoostActive) return;
        
        _damagePerTickBonus = 0;
    }

    //---------------- Talent 4 Logic: Healing Boost ----------------
    public void EnableHealingBoost(bool value)
    {
        _talentHealingBoostActive = value;
        
        if(Hero == null || Hero.DamageTracker == null) return;
        
        Hero.DamageTracker.RemoveOldLocalEntries();
    }

    public void EnableTiredSoulEvade(bool value)
    {
        _talentTiredSoulActive = value;
    }
    
    /*//-------------Talent 9 Logic: Reflection Shield -----------
    private void OnShieldDamageTaken(float damageValue, DamageType damageType, Skill sourceSkill)
    {
        if (!_shieldAttackTalentActive) return;
        if(damageType != Info.DamageType) return;
        if (damageValue <= 0f) return;
        
        if (sourceSkill == null || sourceSkill.Hero == null) return;

        Character attacker = sourceSkill.Hero;
        if (attacker.IsDead) return;

        CmdReflectDamage(attacker.gameObject, damageValue, damageType);
    }*/

    [Command]
    private void CmdReflectDamage(GameObject attacker, float damageValue, DamageType damageType)
    {
        if (attacker == null) return;
        if (!attacker.TryGetComponent<Character>(out var attackerCharacter)) return;
        if (attackerCharacter.IsDead) return;

        Damage reflectDamage = new Damage
        {
            Value = damageValue,
            Type  = damageType,
        };

        ApplyDamage(reflectDamage, attacker);
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        // _targetCharacter = null;
        TargetInfo targetInfo = new();

        while (Targeting.GetTempTarget()?.Character == null)
        {
            if (GetMouseButton)
            {
                Vector3 clickPoint = Targeting.GetMousePoint();

                Targeting.FindTempTarget(clickPoint, _clickRadius, canTargetSelf: true);

                if (Targeting.GetTempTarget().Character is Character character)
                {
                    if (Targeting.GetTempTarget().Character != null && !IsAllyTarget(character))
                    {
                        Targeting.ClearTempTarget();
                    }
                    else
                    {
                        Targeting.GetTempTarget().Character.SelectedCircle.IsActive = true;
                        _hero.Move.LookAtTransform(Targeting.GetTempTarget().Character.transform);
                    }
                }
            }

            yield return null;
        }
        
        Targeting.SetTarget(Targeting.GetTempTarget()?.Character);
        targetInfo.AddTarget(Targeting.GetTarget()?.Character);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (Targeting.GetTarget()?.Character == null || !IsCanCast) yield break;
        Cast();

        yield return null;
    }

    private void Cast()
    {

        CmdPlayShootSound();
        HandleLightShield();
    }

    private void HandleLightShield()
    {
        var target = Targeting.GetTarget()?.Character;
        if (target == null) return;

        var state = target.GetComponent<CharacterState>();
        if (state.CheckForState(States.TiredSoul))
        {
            return;
        }

        IncreaseSetCooldown(CooldownTime);

        CmdAddDebaff(States.LightShield, States.TiredSoul, lightShieldDuration, tiredSoulDuration, absorbAmount, target.gameObject, Name);
    }

    private float BoostActive(float amount, float unit, float boostPerUnit, float maxBoostPercentage)
    {
        float boost = Mathf.Min(Mathf.Floor(amount / unit) * boostPerUnit, absorbAmount * maxBoostPercentage);
        return boost;
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
        float finalAbsorb = damageToExit;

        if (_talentDarkMagicBoostActive)
        {
            float darkDamage = GetAccumulated(_lastDarkDamageTime, DarkDamageResetTime, _accumulatedDarkDamage);
            finalAbsorb += BoostActive(darkDamage, DarkMagicUnit, DarkMagicBoostPerUnit, MaxDarkMagicBoostPercentage);
            _accumulatedDarkDamage = 0f;
            _lastDarkDamageTime = -999f;
        }

        if (_talentPhysicalShieldBoostActive)
        {
            float physicalDamage = GetAccumulated(_lastPhysDamageTime, DarkDamageResetTime, _physDamageAccumulator);
            finalAbsorb += BoostActive(physicalDamage, PhysicUnit, PhysicalBoostPerDamageUnit, MaxPhysicalBoostPercentage);
            _physDamageAccumulator = 0f;
            _lastPhysDamageTime = -999f;
        }

        if (_talentHealingBoostActive)
        {
            float healingAmount = GetAccumulated(_lastHealingTime, DarkDamageResetTime, _healingAccumulator);
            finalAbsorb += BoostActive(healingAmount, HealingUnit, HealingBoostPerUnit, MaxHealingBoostPercentage);
            _healingAccumulator = 0f;
            _lastHealingTime = -999f;
        }

        if (!_talentTiredSoulActive)
        {
            if (characterState.CheckForState(tiredState))
            {
                Debug.Log("Cannot apply Light Shield, target already has TiredSoul and talent is inactive.");
                return;
            }

            Debug.Log("Talent is inactive, applying LightShield and TiredSoul.");
            characterState.AddState(lightState, duration, finalAbsorb, Hero.gameObject, skillName);
            characterState.AddState(tiredState, tiredDuration, finalAbsorb, Hero.gameObject, skillName);
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
        Targeting.ClearTarget();
        //_target = null;
        _damagePerTickBonus = 0;
        _absorbBonus = 0;
    }
}
