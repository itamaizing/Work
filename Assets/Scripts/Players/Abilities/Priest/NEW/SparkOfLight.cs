using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SparkOfLight : AutoAttackSkill
{
    [Header("Spark Of Light Settings")]
    [SerializeField] private float _buffDuration = 9f;
    [SerializeField] private float _healAmount = 2f;
    [SerializeField] private float _damageAmount = 2f;
    [SerializeField] private float _castTime = 0.8f;
    [SerializeField] private float _range = 4f;
    [SerializeField] private List<SkillEnergyCost> _manaCostHeal;
    [SerializeField] private List<SkillEnergyCost> _manaCostDamage;

    [Header("Alternative Mode Settings")]
    [SerializeField] private float _altRange = 6f;
    [SerializeField] private float _altBuffDuration = 5f;
    [SerializeField] private float _altDamageAmount = 2f;
    [SerializeField] private List<SkillEnergyCost> _altManaCostDamage;
    [SerializeField] private FlashOfLight _flashOfLight;

    [SerializeField] private LightSparkProjectile lightSparkProjectile;
    [SerializeField] private HeroComponent playerLinks;
    [SerializeField] private GameObject spawnPoint;
    [SerializeField] private AudioClip audioClip;

    private AudioSource _audioSource;

    [SyncVar(hook = nameof(OnLightModeChanged))] public bool IsLightMode = true;

    private bool _healthBoostActive = false;
    private bool _lowHealthTalentActive = false;
    private bool _manaRestoreBoostTalent = false;
    private bool _healingBuffTalentActive = false;

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

    public void EnableTalentPhysicalShieldBoost(bool value) => _healthBoostActive = value;
    public void EnableLowHealthTalent(bool value) => _lowHealthTalentActive = value;
    public void EnableManaRestoreBoostTalent(bool value) => _manaRestoreBoostTalent = value;
    public void EnableHealingBuffTalent(bool value) => _healingBuffTalentActive = value;

    private bool IsAllyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");
    private bool IsEnemyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Enemy");

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerAutoAttack => Animator.StringToHash("SparkOfLights");

    public event Action OnModeChange;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        _flashOfLight.CastEnded += HandleLastTimeFlashOfLightCast;
        OnModeChange += HandleModeChange;
        UpdateMode();
    }

    private void OnDisable()
    {
        _flashOfLight.CastEnded -= HandleLastTimeFlashOfLightCast;
        OnModeChange -= HandleModeChange;
    }

    public void SwitchMode()
    {
        CmdSwitchMode();
    }

    [Command]
    private void CmdSwitchMode()
    {
        IsLightMode = !IsLightMode;
    }

    private void OnLightModeChanged(bool oldValue, bool newValue)
    {
        OnModeChange?.Invoke();
        UpdateMode();
    }

    private void HandleModeChange()
    {
        UpdateMode();
    }

    private void HandleLastTimeFlashOfLightCast()
    {
        _lastFlashOfLightCastTime = Time.time;
    }

    private void UpdateMode()
    {
        School = IsLightMode ? Schools.Light : Schools.Dark;
    }

    protected override void CastAction()
    {
        if (_target == null) return;

        if (IsAllyTarget(_target))
        {
            TryPayCost(_manaCostHeal);

            if (_target == playerLinks)
            {
                CmdHandleDefaultMode(playerLinks);
                return;
            }
        }

        else if (IsEnemyTarget(_target)) TryPayCost(_manaCostDamage);

        CmdSpawnProjectile(_target.gameObject);
    }

    public void HandleMode(Character target)
    {
        if (IsLightMode) HandleDefaultMode(target);
        else HandleAlternativeMode(target);
    }

    private bool IsTargetBelowHealthThreshold(Character target)
    {
        var healthComponent = target.GetComponent<Health>();
        return healthComponent != null && healthComponent.CurrentValue <= healthComponent.MaxValue * LowHealthThreshold;
    }

    [Command]
    private void CmdHandleDefaultMode(Character target)
    {
        HandleDefaultMode(playerLinks);
        RpcPlayShotSound();
    }

    private void HandleDefaultMode(Character target)
    {
        if (IsAllyTarget(target))
        {
            Heal(target);
            ApplySpiritEnergyBuff(target);
            //ApplyHealthBuff(_target);
        }
        else if (IsEnemyTarget(target))
        {
            DamageCast(target);
        }
    }

    private void HandleAlternativeMode(Character target)
    {
        if (IsEnemyTarget(target))
        {
            ApplyDamageInAltMode(target);
            ApplySpiritHealthBuff(target);

            if (_lowHealthTalentActive && IsTargetBelowHealthThreshold(target))
            {
                ApplyDefenseDebuff(target);
            }
        }

        Debug.Log("HandleAlternativeMode");
    }

    private void Heal(Character target)
    {
        var isBonusActive = _healingBuffTalentActive && Time.time < _lastFlashOfLightCastTime + _healingBuffDuration;

        if (isBonusActive)
        {
            _healingBonusStacks++;
        }
        else
        {
            _healingBonusStacks = 0;
        }

        var bonus = isBonusActive ? _tickHealingBonus * _healingBonusStacks : 0;

        var heal = new Heal { Value = _healAmount + bonus };
        ApplyHeal(heal, target.gameObject, this, Name);
    }

    private void DamageCast(Character target)
    {
        ApplyDamage(CreateDamage(_damageAmount), target.gameObject);
    }

    private void ApplyDamageInAltMode(Character target)
    {
        float damageAmount = _altDamageAmount;
        if (_lowHealthTalentActive && IsTargetBelowHealthThreshold(target))
        {
            damageAmount *= BonusDamageMultiplier;
        }

        ApplyDamage(CreateDamage(damageAmount), target.gameObject);
    }

    private Damage CreateDamage(float amount)
    {
        return new Damage
        {
            Value = Buff.Damage.GetBuffedValue(amount),
            Type = DamageType.Magical,
            PhysicAttackType = AttackRangeType.RangeAttack,
            School = this.School,
            //DamageableSkill = this,
        };
    }

    private void ApplySpiritEnergyBuff(Character target)
    {
        var talentActive = _manaRestoreBoostTalent ? 1 : 0;
        AddBuff(States.SpiritEnergy, _buffDuration, talentActive, target.gameObject, Name);
    }

    private void ApplySpiritHealthBuff(Character target)
    {
        var talentActive = _manaRestoreBoostTalent ? 1 : 0;
        AddBuff(States.SpiritHealth, _altBuffDuration, talentActive, target.gameObject, Name);
    }

    private void ApplyHealthBuff(Character target)
    {
        if (!_healthBoostActive) return;

        AddBuff(States.SparkTalentHealthBuff, HealthBoostDuration, HealthBoostPercentage, target.gameObject, Name);
    }

    private void ApplyDefenseDebuff(Character target)
    {
        AddBuff(States.DefenseReduction, DefenseDebuffDuration, DefenseReductionPercentage, target.gameObject, Name);
    }

    public void SparkOfLightCast()
    {
        AnimCastAction();
    }

    public void SparkOfLightEnded()
    {
        AnimCastEnded();
    }

    [Command]
    private void CmdSpawnProjectile(GameObject target)
    {
        Vector3 targetPosition = target.transform.position + Vector3.up;
        Vector3 direction = (targetPosition - spawnPoint.transform.position).normalized;
        float distance = Vector3.Distance(targetPosition, spawnPoint.transform.position);

        LightSparkProjectile projectile = Instantiate(lightSparkProjectile, spawnPoint.transform.position, Quaternion.LookRotation(direction));

        float attackDelay = _castTime;

        projectile.Init(playerLinks, IsLightMode, this, distance, attackDelay, target.transform);

        SceneManager.MoveGameObjectToScene(projectile.gameObject, _hero.NetworkSettings.MyRoom);
        NetworkServer.Spawn(projectile.gameObject);
        projectile.StartFly(direction);

        RpcInitProjectile(projectile.gameObject, distance, attackDelay, target);
        RpcPlayShotSound();
    }


    private void AddBuff(States state, float duration, float modifier, GameObject target, string skillName)
    {
        var characterState = target.GetComponent<CharacterState>();
        characterState.AddState(state, duration, modifier, target, skillName);
    }

    [ClientRpc]
    private void RpcInitProjectile(GameObject projectileObject, float distance, float attackDelay, GameObject target)
    {
        if (projectileObject.TryGetComponent(out LightSparkProjectile projectile))
        {
            projectile.Init(playerLinks, IsLightMode, this, distance, attackDelay, target.transform);
        }
    }

    [ClientRpc]
    private void RpcPlayShotSound()
    {
        if (_audioSource != null && audioClip != null) _audioSource.PlayOneShot(audioClip);
    }

    protected override void ClearData()
    {
        base.ClearData();
    }
}