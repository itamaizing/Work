using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SparkOfLight : Skill,IPolaritySwitchable
{
    [Header("Spark Of Light Settings")]
    [SerializeField] private float _buffDuration = 9f;
    [SerializeField] private float _healAmount = 2f;
    [SerializeField] private float _damageAmount = 2f;
    [SerializeField] private float _castTime = 0.8f;
    [SerializeField] private float _range = 4f;
    
    [SerializeField] private AbilityInfo lightInfo;

    [Header("Alternative Mode Settings")]
    [SerializeField] private float _altRange = 6f;
    [SerializeField] private float _altBuffDuration = 5f;
    [SerializeField] private float _altDamageAmount = 2f;
    [SerializeField] private FlashOfLight _flashOfLight;
    [SerializeField] private AbilityInfo darkInfo;

    [SerializeField] private LightSparkProjectile lightSparkProjectile;
    [SerializeField] private LightSparkProjectile darkSparkProjectile;
    [SerializeField] private HeroComponent playerLinks;
    [SerializeField] private GameObject spawnPoint;
    [SerializeField] private AudioClip audioClip;
    [SerializeField] private StunMagicPassiveSkill stunMagicPassiveSkill;

    private AudioSource _audioSource;
    private bool _spiritEnergyAddTalent;

    [SyncVar(hook = nameof(OnModeChanged))] public bool isLightMode = true;
    public bool IsLightMode => isLightMode;

    private bool _healthBoostActive = false;
    private bool _lowHealthTalentActive = false;
    private bool _manaRestoreBoostTalent = false;
    private bool _healingBuffTalentActive = false;
    private bool _spiritEnergyTalent;

    private const float LowHealthThreshold = 0.25f;
    private const float BonusDamageMultiplier = 1.25f;
    private const float HealthBoostPercentage = 0.25f;
    private const float HealthBoostDuration = 2f;
    private const float DefenseReductionPercentage = 0.25f;
    private const float DefenseDebuffDuration = 2f;

    private float _healingBuffDuration = 5f;
    private float _tickHealingBonus = 2f;
    private int _healingBonusStacks = 0;
    private float _lastFlashOfLightCastTime = 0f;
    
    private float _clickRadius = 0.5f;

    //protected IDamageable _target;
    //private Character _characterTarget;

    public void EnableTalentPhysicalShieldBoost(bool value) => _healthBoostActive = value;
    public void EnableLowHealthTalent(bool value) => _lowHealthTalentActive = value;
    public void EnableManaRestoreBoostTalent(bool value) => _manaRestoreBoostTalent = value;
    public void EnableHealingBuffTalent(bool value) => _healingBuffTalentActive = value;


    private bool IsAllyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");
    private bool IsEnemyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Enemy");
    
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("SparkOfLights");

    protected override bool IsCanCast => Targeting.GetTarget()?.Character != null && Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position) <= AreaInfo.Radius && Targeting.NoObstacles(Targeting.GetTarget().Character.transform.position, transform.position, _obstacle);

    public event Action OnModeChange;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        _flashOfLight.CastEnded += HandleLastTimeFlashOfLightCast;
        OnModeChange += UpdateMode;
        UpdateMode();
    }

    private void OnDisable()
    {
        _flashOfLight.CastEnded -= HandleLastTimeFlashOfLightCast;
        OnModeChange -= UpdateMode;
    }

    public void SwitchMode()
    {
        CmdSwitchMode();
    }

    private void OnModeChanged(bool oldValue, bool newValue)
    {
        UpdateMode();
        OnModeChange?.Invoke();
    }

    private void HandleLastTimeFlashOfLightCast()
    {
        _lastFlashOfLightCastTime = Time.time;
    }

    private void UpdateMode()
    {
        Info.School = isLightMode ? Schools.Light : Schools.Dark;
        AbilityInfoHero = isLightMode ? lightInfo : darkInfo;

        ClearData();
        Targeting.ClearTarget();
        //_characterTarget = null;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {

        while (Targeting.GetTempTarget()?.Character == null)
        {
            if (GetMouseButton)
            {
                Vector3 clickPoint = Targeting.GetMousePoint();

                Targeting.FindTempTarget(clickPoint, _clickRadius, canTargetSelf: true);

                if (Targeting.GetTempTarget()?.Character is Character character)
                {
                    if (Targeting.GetTempTarget()?.Character != null && (IsAllyTarget(character)) && !isLightMode)
                    {
                        Targeting.ClearTempTarget();
                    }

                    if (Targeting.GetTempTarget()?.Character != null)
                    {
                        character.SelectedCircle.IsActive = true;
                    }
                }
            }

            yield return null;
        }

        Targeting.SetTarget(Targeting.GetTempTarget()?.Character);
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Targeting.GetTarget()?.Character);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (Targeting.GetTarget()?.Character == null) yield break;
        
        GameObject target = Targeting.GetTarget()?.Character.gameObject;
        CmdSpawnProjectile(target, isLightMode);
        ClearData();
    }

    private bool IsTargetBelowHealthThreshold(Character target)
    {
        var healthComponent = target.GetComponent<Health>();
        return healthComponent != null && healthComponent.CurrentValue <= healthComponent.MaxValue * LowHealthThreshold;
    }

    private void TryApplyExtraState(Character target)
    {
        if (!stunMagicPassiveSkill.IsFillingDestruction || target == null) return;

        var stateComponent = target.GetComponent<CharacterState>();
        if (stateComponent == null) return;

        if (!isLightMode && (UnityEngine.Random.value <= 0.2f)) stateComponent.AddState(States.Destruction, 12f, 0, gameObject, Name);
    }
    
    private void TryApplyDestructionFilling(Character target)
    {
        if (target == null) return;
        if(IsEnemyTarget(target) && isLightMode) return; 
        
        CharacterState targetState = target.CharacterState;
        if (UnityEngine.Random.value <= _destructionFillingChance)
        {
            float durationToApply = targetState.CheckForState(isLightMode ? States.Restoration : States.Destruction) ? _destructionFillingExtensionTime : _destructionFillingDuration;
            CmdStateRestorationOrDestruction(targetState,isLightMode ? States.Restoration : States.Destruction, durationToApply);
        }
    }

    private void CmdStateRestorationOrDestruction(CharacterState stateComponent, States states, float duration) => stateComponent.AddState(states, duration, 1f, gameObject, Name);
    
    private void HandleDefaultMode(Character target)
    {
        if (target == null) return;

        if (IsAllyTarget(target) || target == _hero)
        {
            Heal(target);
            ApplySpiritEnergyBuff(target);
        }
        if (IsEnemyTarget(target))
        {
            ApplyDamageInAltMode(target);
        }
    }

    private void HandleAlternativeMode(Character target)
    {
        if (target == null) return;

        if (IsEnemyTarget(target))
        {
            ApplyDamageInAltMode(target);
            ApplySpiritHealthBuff(target);

            if (_lowHealthTalentActive && IsTargetBelowHealthThreshold(target))
                ApplyDefenseDebuff(target);
        }
    }

    private void Heal(Character target)
    {
        bool isBonusActive = _healingBuffTalentActive && Time.time < _lastFlashOfLightCastTime + _healingBuffDuration;

        if (isBonusActive) _healingBonusStacks = Mathf.Min(_healingBonusStacks + 1, 4);
        else _healingBonusStacks = 0;

        float doublingBonus = (_healingBonusStacks > 0) ? Mathf.Pow(2f, _healingBonusStacks) : 0f;

        float bonusHealFromSpiritEnergy = _spiritEnergyTalent ? GetSpiritEnergyBonus(target) : 0f;

        var heal = new Heal
        {
            Value = _healAmount + doublingBonus + bonusHealFromSpiritEnergy,
            DamageableSkill = this
        };
        
        ApplyHeal(heal, target.gameObject, this, Name);

        TryApplyExtraState(target);
        if (_isDestructionFillingTalent) TryApplyDestructionFilling(target);
    }

    private float GetSpiritEnergyBonus(Character target)
    {
        var characterState = target?.GetComponent<CharacterState>();
        if (characterState == null) return 0f;

        var spiritEnergyState = characterState.GetState(States.SpiritEnergy) as SpiritEnergyState;
        return spiritEnergyState != null ? spiritEnergyState.GetHealBonus() : 0f;
    }

    private void ApplyDamageInAltMode(Character target)
    {
        float damageAmount = isLightMode ? _damageAmount : _altDamageAmount;

        if (_lowHealthTalentActive && IsTargetBelowHealthThreshold(target))
            damageAmount *= BonusDamageMultiplier;

        Damage damage = CreateDamage(damageAmount);

        ApplyDamage(damage, target.gameObject);

        TryApplyExtraState(target);
        if (_isDestructionFillingTalent) TryApplyDestructionFilling(target);
    }

    private Damage CreateDamage(float amount)
    {
        return new Damage
        {
            Value = Buff.Damage.GetBuffedValue(amount),
            Type = DamageType.Magical,
            PhysicAttackType = AttackRangeType.RangeAttack,
            School = this.Info.School,
            //DamageableSkill = this,
        };
    }

    private void ApplySpiritEnergyBuff(Character target)
    {
        var talentActive = _manaRestoreBoostTalent ? 1 : 0;
        if (_spiritEnergyAddTalent) AddBuff(States.SpiritEnergy, _buffDuration, talentActive, target.gameObject, Name);
    }

    private void ApplySpiritHealthBuff(Character target)
    {
        var talentActive = _manaRestoreBoostTalent ? 1 : 0;
        if (_spiritEnergyAddTalent) AddBuff(States.SpiritHealth, _altBuffDuration, talentActive, target.gameObject, Name);
    }

    private void ApplyDefenseDebuff(Character target)
    {
        //AddBuff(States.DefenseReduction, DefenseDebuffDuration, DefenseReductionPercentage, target.gameObject, Name);
    }

    public void SparkOfLightCast()
    {
        AnimStartCastCoroutine();
    }

    public void SparkOfLightEnded()
    {
        AnimCastEnded();
    }

    #region Talents
    
    private float _destructionFillingExtensionTime;
    private float _destructionFillingDuration;
    private float _destructionFillingChance;
    
    private bool _isDestructionFillingTalent;
    public bool IsDestructionFillingTalent { get => _isDestructionFillingTalent;private set => _isDestructionFillingTalent = value; }
    
    public void SpiritEnergyTalentActive(bool value) => _spiritEnergyTalent = value;

    public void SpiritEnergyAddTalent(bool value) => _spiritEnergyAddTalent = value;

    [Command]
    public void CmdSetDestructionFillingTalent(bool value, float duration, float additionalTime, float chance)
    {
        DestructionFillingTalent(value, duration, additionalTime, chance);
    }
    
    public void DestructionFillingTalent(bool value, float duration, float additionalTime,float chance)
    {
        _isDestructionFillingTalent = value;
        _destructionFillingExtensionTime = additionalTime;
        _destructionFillingDuration = duration;
        _destructionFillingChance = chance;
    }
    
    
    #endregion

    private void AddBuff(States state, float duration, float modifier, GameObject target, string skillName)
    {
        var characterState = target.GetComponent<CharacterState>();
        characterState.AddState(state, duration, modifier, target, skillName);
    }

    [Command]
    private void CmdSpawnProjectile(GameObject target, bool isLight)
    {
        Vector3 targetPosition = target.transform.position + Vector3.up;
        Vector3 direction = (targetPosition - spawnPoint.transform.position).normalized;

        LightSparkProjectile projectile = Instantiate(
            isLight ? lightSparkProjectile : darkSparkProjectile,
            spawnPoint.transform.position,
            Quaternion.LookRotation(direction));

        projectile.Init(target);

        SceneManager.MoveGameObjectToScene(projectile.gameObject, _hero.NetworkSettings.MyRoom);
        projectile.EndPointReached += OnEndPointReached;
        NetworkServer.Spawn(projectile.gameObject);
        projectile.StartFly();

        RpcPlayShotSound();
    }
    
    private void OnEndPointReached(LightSparkProjectile projectile, GameObject target)
    {
        projectile.EndPointReached -= OnEndPointReached;

        if (isLightMode) HandleDefaultMode(target.GetComponent<Character>());
        else HandleAlternativeMode(target.GetComponent<Character>());
    }


    [Command]
    private void CmdSwitchMode()
    {
        isLightMode = !isLightMode;
        UpdateMode();
    }

    [ClientRpc]
    private void RpcInitProjectile(GameObject projectileObject, GameObject target)
    {
        if (projectileObject.TryGetComponent(out LightSparkProjectile projectile))
        {
            projectile.Init(target);
        }
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

        _hero.Move.StopLookAt();
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0)
            Targeting.SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
    }
}

