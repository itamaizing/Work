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
    private bool _stackingRestorationTalent = false;
    private bool _stackingDestructionTalent = false;

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

    public void EnableTalentPhysicalShieldBoost(bool value) => _healthBoostActive = value;
    public void EnableLowHealthTalent(bool value) => _lowHealthTalentActive = value;
    public void EnableManaRestoreBoostTalent(bool value) => _manaRestoreBoostTalent = value;
    public void EnableHealingBuffTalent(bool value) => _healingBuffTalentActive = value;


    private bool IsAllyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");
    private bool IsEnemyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Enemy");
    
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("SparkOfLights");

    protected override bool IsCanCast => Targeting.GetTarget()?.Character != null && Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position) <= AreaInfo.Radius && Targeting.NoObstacles(Targeting.GetTarget().Character.transform.position, transform.position, _obstacle);

    public void SetStackingRestorationTalent(bool value) => _stackingRestorationTalent = value;
    public void SetStackingDestructionTalent(bool value) => _stackingDestructionTalent = value;
    
    #region AoeTalent
    private bool _aoeTalentActiveServer = false;
    private const float _aoeRadius = 1f;
    private const float _aoeDamagePercent = 0.3f;
    private const float _aoeHealPercent = 0.3f;

    public void SetAoeTalent(bool value)
    {
        _aoeTalentActiveServer = value;
        if(isClient)
            CmdSetAoeTalent(value);
    }

    [Command]
    private void CmdSetAoeTalent(bool value)
    {
        _aoeTalentActiveServer = value;
    }
    #endregion
    #region SpiritHealthOnShadow
    private bool _spiritHealthIsEnabled;
    public bool EnableSpiritHealth(bool val) => _spiritHealthIsEnabled = val;
    #endregion

    #region InstantFlashOfLight

    private InstantFlashBooster _instantFlash;

    public InstantFlashBooster InstantFlashBooster => _instantFlash;

    #endregion
    
    #region OverhealManaBooster

    private OverhealManaBooster _overhealMana;
    public OverhealManaBooster OverhealManaBooster => _overhealMana;

    #endregion
    
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
        
        _instantFlash = new InstantFlashBooster(this, duration: 5f, chance: 10f);
        var flashSkill = Hero.Abilities.GetSkill<FlashOfLight>();;
        _instantFlash.Inject(flashSkill);
        
        _overhealMana = new OverhealManaBooster(this, Hero);
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

        if (!isLightMode && (UnityEngine.Random.value <= 0.2f)) stateComponent.AddState(States.Destruction, 12f, 0, gameObject, nameof(SparkOfLight));
    }
    
    private void TryApplyDestructionFilling(Character target)
    {
        if (target == null) return;
        if (IsEnemyTarget(target) && isLightMode) return;
        if (UnityEngine.Random.value > _destructionFillingChance) return;

        CharacterState targetState = target.CharacterState;
        States stateToUse = isLightMode
            ? (_stackingRestorationTalent ? States.RestorationStacking : States.Restoration)
            : (_stackingDestructionTalent ? States.DestructionStacking : States.Destruction);

        float duration = targetState.CheckForState(stateToUse)
            ? _destructionFillingExtensionTime
            : _destructionFillingDuration;

        CmdStateRestorationOrDestruction(targetState, stateToUse, duration);
    }

    private void CmdStateRestorationOrDestruction(CharacterState stateComponent, States states, float duration)
    {
        float damageToExit = 0;
        if (_spiritHealthIsEnabled && (states == States.Destruction || states == States.DestructionStacking))
        {
            damageToExit = -1f;
        }
        stateComponent.AddState(states, duration, damageToExit, gameObject, nameof(SparkOfLight));
    }

    private void HandleDefaultMode(Character target)
    {
        if (target == null) return;

        if (IsAllyTarget(target) || target == _hero)
        {
            float healValue = Heal(target,_healAmount);
            ApplySpiritEnergyBuff(target);
            RpcApplyInstant();
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
            
            RpcApplyInstant();
        }
    }

    private float Heal(Character target,float healVal)
    {
        bool isBonusActive = _healingBuffTalentActive && Time.time < _lastFlashOfLightCastTime + _healingBuffDuration;

        if (isBonusActive) _healingBonusStacks = Mathf.Min(_healingBonusStacks + 1, 4);
        else _healingBonusStacks = 0;

        float doublingBonus = (_healingBonusStacks > 0) ? Mathf.Pow(2f, _healingBonusStacks) : 0f;
        float bonusHealFromSpiritEnergy = _spiritEnergyTalent ? GetSpiritEnergyBonus(target) : 0f;
        float healValue = healVal + doublingBonus + bonusHealFromSpiritEnergy;

        var heal = new Heal { Value = healValue, DamageableSkill = this };
        ApplyHeal(heal, target.gameObject, this, nameof(SparkOfLight));
        TryApplyExtraState(target);
        if (_isDestructionFillingTalent) TryApplyDestructionFilling(target);

        OnOverhealHeal(target.gameObject, healValue);
        return healValue;
    }

    [ClientRpc]
    private void OnOverhealHeal(GameObject target, float healValue)
    {
        target.TryGetComponent(out Character c);
        if(c)
            _overhealMana.OnAnyHealTaken(c,healValue,this);
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
        
        ApplyDamage(CreateDamage(damageAmount), target.gameObject);
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
            School = this.Info.School
        };
    }
    
    private void ApplyAoeEffect(Character mainTarget, float value, bool isDamage)
    {
        float aoeValue = value * (isDamage ? _aoeDamagePercent : _aoeHealPercent);
        Collider[] hits = Physics.OverlapSphere(mainTarget.transform.position, _aoeRadius, Targeting.Layer);

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<Character>(out var target)) continue;
            if (target == mainTarget || target.IsDead) continue;

            if (isDamage)
            {
                if (!IsEnemyTarget(target)) continue;
                CmdApplyDamage(CreateDamage(aoeValue), target.gameObject);
            }
            else
            {
                if (!IsAllyTarget(target) && target != _hero) continue;
                Heal(target,aoeValue);
            }

            TryApplyExtraState(target);
            if (_isDestructionFillingTalent) TryApplyDestructionFilling(target);
        }
    }


    private void ApplySpiritEnergyBuff(Character target)
    {
        var talentActive = _manaRestoreBoostTalent ? 1 : 0;
        if (_spiritEnergyAddTalent) AddBuff(States.SpiritEnergy, _buffDuration, talentActive, target.gameObject, nameof(SparkOfLight));
    }

    private void ApplySpiritHealthBuff(Character target)
    {
        var talentActive = _manaRestoreBoostTalent ? 1 : 0;
        if (_spiritEnergyAddTalent) AddBuff(States.SpiritHealth, _altBuffDuration, talentActive, target.gameObject, nameof(SparkOfLight));
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
        characterState.AddState(state, duration, modifier, gameObject, skillName);
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
        //SceneManager.MoveGameObjectToScene(projectile.gameObject, _hero.NetworkSettings.MyRoom);

        projectile.EndPointReached += (proj, tgt) =>
        {
            var character = tgt.GetComponent<Character>();

            if (isLight) HandleDefaultMode(character);
            else HandleAlternativeMode(character);
            
            TargetRpcApplyAoe(connectionToClient, tgt, isLight);
        };

        NetworkServer.Spawn(projectile.gameObject);
        projectile.StartFly();
        RpcPlayShotSound();
    }

    [ClientRpc]
    private void RpcApplyInstant()
    {
        if(isOwned)
            _instantFlash.TryApply();
    }
    
    [TargetRpc]
    private void TargetRpcApplyAoe(NetworkConnectionToClient conn, GameObject targetGO, bool isLight)
    {
        if (!_aoeTalentActiveServer) return;

        var target = targetGO.GetComponent<Character>();
        if (target == null) return;

        if (isLight)
        {
            ApplyAoeEffect(target, _healAmount, false);
        }
        else
        {
            float dmg = isLightMode ? _damageAmount : _altDamageAmount;
            ApplyAoeEffect(target, isLightMode ? _damageAmount : _altDamageAmount, true);
        }
    }
    
    [Command]
    private void CmdSwitchMode()
    {
        isLightMode = !isLightMode;
        UpdateMode();
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

