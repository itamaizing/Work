using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class PriestShield : Skill
{
    [SerializeField] private float lightShieldDuration = 18f;
    [SerializeField] private float tiredSoulDuration = 12f;
    [SerializeField] private float absorbAmount = 40f;
    [SerializeField] private AudioClip audioClip;

    private AudioSource _audioSource;

    private float _damagePerTickBonus = 0;

    private float _nextAvailableTime;
    private float _clickRadius = 0.5f;

    protected override int AnimTriggerCastDelay => Animator.StringToHash("PriestShield");
    protected override int AnimTriggerCast => 0;
    
    private bool IsAllyTarget(Character target) => target != null && target.gameObject.layer == LayerMask.NameToLayer("Allies");

    public override bool IsPayCostStartCooldown => false;

    private readonly Dictionary<PriestShieldBoosterType, SkillTalentHandler> _boosters = new();

    #region Boosters

    #region Enums

    public enum PriestShieldBoosterType
    {
        SpiritShieldReflection,
        LightShieldManaRestore,
        HealingBoost,
        DarkMagicBoost,
        PhysicalShieldBoost,
        DisciplineShieldBoost 
    }

    #endregion

    #region Physical Shield Boost Talent
    private PhysicalShieldBoostBooster _physicalShieldBoostBooster;
    public PhysicalShieldBoostBooster PhysicalShieldBoostBooster => _physicalShieldBoostBooster;

    private float _physDamageAccumulator = 0f;
    #endregion
    #region Discipline Shield Boost Talent
    private DisciplineShieldBoostBooster _disciplineShieldBoostBooster;
    public DisciplineShieldBoostBooster DisciplineShieldBoostBooster => _disciplineShieldBoostBooster;

    private float _disciplineBonus = 0f;
    #endregion
    #region Dark Magic Boost Talent
    private DarkMagicBoostBooster _darkMagicBoostBooster;
    public DarkMagicBoostBooster DarkMagicBoostBooster => _darkMagicBoostBooster;

    private float _darkDamageAccumulator = 0f;
    #endregion
    #region Healing Boost Talent
    private HealingBoostBooster _healingBoostBooster;
    public HealingBoostBooster HealingBoostBooster => _healingBoostBooster;

    private float _healAccumulator = 0f;
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

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);
        Hero.DamageTracker.OnDamageTracked += TrackDarkDamage;
        Hero.Health.DamageTaken += TrackPhysDamage;
        Hero.DamageTracker.OnHealTracked += TrackHealDone;

        foreach (var skill in Hero.Abilities.Abilities.Where(skill => skill.Info.School == Schools.Discipline))
            skill.CastEnded += AddDisciplineStack;
    }

    private void OnEnable()
    {        
        _spiritShieldReflectionBooster = new SpiritShieldReflectionBooster(this);
        _lightShieldManaBooster = new LightShieldManaRestoreBooster(this);
        _healingBoostBooster = new HealingBoostBooster(this);
        _darkMagicBoostBooster = new DarkMagicBoostBooster(this);
        _physicalShieldBoostBooster = new PhysicalShieldBoostBooster(this);
        _disciplineShieldBoostBooster = new DisciplineShieldBoostBooster(this);

        RegisterBooster(PriestShieldBoosterType.DisciplineShieldBoost, _disciplineShieldBoostBooster);
        RegisterBooster(PriestShieldBoosterType.PhysicalShieldBoost, _physicalShieldBoostBooster);
        RegisterBooster(PriestShieldBoosterType.DarkMagicBoost, _darkMagicBoostBooster);
        RegisterBooster(PriestShieldBoosterType.HealingBoost, _healingBoostBooster);
        RegisterBooster(PriestShieldBoosterType.SpiritShieldReflection, _spiritShieldReflectionBooster);
        RegisterBooster(PriestShieldBoosterType.LightShieldManaRestore, _lightShieldManaBooster);
    }

    private void OnDisable()
    {
        Hero.DamageTracker.OnDamageTracked -= TrackDarkDamage;
        Hero.Health.DamageTaken -= TrackPhysDamage;
        Hero.DamageTracker.OnHealTracked -= TrackHealDone;

        foreach (var skill in Hero.Abilities.Abilities.Where(skill => skill.Info.School == Schools.Discipline))
            skill.CastEnded -= AddDisciplineStack;
    }

    private void RegisterBooster(PriestShieldBoosterType type, SkillTalentHandler booster)
    {
        _boosters[type] = booster;
    }
    
    public void EnableBooster(PriestShieldBoosterType type, bool value)
    {
        if (!isClient) return;
        CmdEnableBooster(type, value);
    }

    [Command]
    private void CmdEnableBooster(PriestShieldBoosterType type, bool value)
    {
        if (_boosters.TryGetValue(type, out var booster))
        {
            booster.Enable(value);
        }
    }
    
    public void EnableReflectionBooster(bool value) => EnableBooster(PriestShieldBoosterType.SpiritShieldReflection, value);

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

        _darkMagicBoostBooster?.OnDarkDamageDone(damage);
    }

    private void TrackPhysDamage(Damage damage, Skill skill)
    {
        if (damage.School != Schools.Physical) return;

        _physicalShieldBoostBooster?.OnPhysicalDamageTaken(damage);
    }

    private void TrackHealDone(Heal heal)
    {
        if (heal.DamageableSkill == null) return;
        if (heal.DamageableSkill.Info.School != Schools.Light) return;

        _healingBoostBooster?.OnHealDone(heal);
    }
    
    private void AddDisciplineStack()
    {
        _disciplineShieldBoostBooster?.OnDisciplineSkillCast();
    }
    
    public void SetHealingBoostValue(float boostValue) => _healAccumulator = boostValue;

    public void SetDarkMagicBoostValue(float boostValue) => _darkDamageAccumulator = boostValue;

    public void SetPhysicalShieldBoostValue(float boostValue) => _physDamageAccumulator = boostValue;
    
    public void SetDisciplineShieldBoostValue(float boostValue) => _disciplineBonus = boostValue;
    #endregion
    
    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
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

        Cooldown.Start();

        CmdAddDebaff(States.LightShield, States.TiredSoul, lightShieldDuration, tiredSoulDuration, absorbAmount, target.gameObject, Name);
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

        if (_healingBoostBooster != null)          finalAbsorb += _healAccumulator;
        if (_darkMagicBoostBooster != null)        finalAbsorb += _darkDamageAccumulator;
        if (_physicalShieldBoostBooster != null)   finalAbsorb += _physDamageAccumulator;
        if (_disciplineShieldBoostBooster != null) finalAbsorb += _disciplineBonus; 

        if (characterState.CheckForState(tiredState)) return;

        characterState.AddState(lightState, duration, finalAbsorb, Hero.gameObject, skillName);
        characterState.AddState(tiredState, tiredDuration, finalAbsorb, Hero.gameObject, skillName);
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
    }
}
