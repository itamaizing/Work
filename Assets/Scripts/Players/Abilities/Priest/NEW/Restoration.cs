using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class Restoration : Skill
{
    [Header("Restoration (Light Mode) Settings")]
    [SerializeField] private float healPerTick = 6f;
    [SerializeField] private float lightRange = 4f;
    [SerializeField] private float lightDuration = 12.1f;
    [SerializeField] private float healInterval = 4f;
    [SerializeField] private float lightCastTime = 1.2f;
    [SerializeField] private float effectivenessIncreasePerHeal = 0.1f;
    [SerializeField] private AbilityInfo lightInfo;

    [Header("Restoration (Dark Mode) Settings")]
    [SerializeField] private float damagePerTick = 6f;
    [SerializeField] private float darkRange = 6f;
    [SerializeField] private float darkDuration = 12.1f;
    [SerializeField] private float damageInterval = 3f;
    [SerializeField] private float darkCastTime = 1.2f;
    [SerializeField] private AbilityInfo darkInfo;

    [SerializeField] private AudioClip audioClip;

    private AudioSource _audioSource;
    private float _accumulatedEffectiveness = 1f;
    private float _totalHealedInInterval = 0f;
    private bool _spiritEnergyTalent;
    private IDamageable _target;
    private Character characterTarget;

    public IDamageable Target => _target;

    [SyncVar(hook = nameof(OnModeChanged))] public bool isLightMode = true;

    protected override bool IsCanCast => IsCanCastCheck();

    protected override int AnimTriggerCastDelay => Animator.StringToHash("Cast");
    protected override int AnimTriggerCast => 0;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    private bool IsCanCastCheck()
    {
        if (_target == null) return false;
        return Vector3.Distance(transform.position, _target.transform.position) <= Radius;
    }

    public event Action OnModeChange;

    private void OnEnable()
    {
        OnModeChange += UpdateMode;
        UpdateMode();
    }

    private void OnDisable()
    {
        OnModeChange -= UpdateMode;
        if (_target != null && _target is Character character)
        {
            var healthComponent = character.GetComponent<Health>();
            if (healthComponent != null)
            {
                healthComponent.HealTaked -= OnHealTaken;
            }
        }
    }


    public void SwitchMode()
    {
        CmdSwitchMode();
    }

    [Command]
    private void CmdSwitchMode()
    {
        UpdateMode();
        isLightMode = !isLightMode;
    }

    private void OnModeChanged(bool oldValue, bool newValue)
    {
        UpdateMode();
        OnModeChange?.Invoke();
    }

    public void SpiritEnergyTalentActive(bool value)
    {
        _spiritEnergyTalent = value;
    }

    private void UpdateMode()
    {
        Radius = isLightMode ? lightRange : darkRange;
        School = isLightMode ? Schools.Light : Schools.Dark;
        CastDeley = isLightMode ? lightCastTime : darkCastTime;
        AbilityInfoHero = isLightMode ? lightInfo : darkInfo;
        TargetsLayers = isLightMode ? LayerMask.GetMask("Allies") : LayerMask.GetMask("Enemy");
        Hero.Abilities.SkillPanelUpdate();
    }

    private void HandleRestorationLight()
    {
        if (characterTarget == null) return;

        bool isAlly = _target.gameObject.layer == LayerMask.NameToLayer("Allies");

        if (isAlly && TryPayCost())
        {
            var healthComponent = characterTarget.GetComponent<Health>();
            if (healthComponent != null)
            {
                healthComponent.HealTaked += OnHealTaken;
            }

            CmdAddState(characterTarget, States.Restoration, lightDuration);
            //StartCoroutine(ApplyHealOverTime(characterTarget));
        }
    }

    private float GetSpiritEnergyBonus(Character target)
    {
        var characterState = target?.GetComponent<CharacterState>();
        if (characterState == null) return 0f;

        var spiritEnergyState = characterState.GetState(States.SpiritEnergy) as SpiritEnergyState;
        return spiritEnergyState != null ? spiritEnergyState.GetHealBonus() : 0f;
    }

    private void HandleRestorationDark()
    {
        if (characterTarget == null) return;

        bool isEnemy = characterTarget.gameObject.layer == LayerMask.NameToLayer("Enemy");

        if (isEnemy && TryPayCost())
        {
            CmdAddState(characterTarget, States.Destruction, darkDuration);
            //StartCoroutine(ApplyDamageOverTime(characterTarget));
        }
    }

    private void OnHealTaken(float healedAmount, Skill skill, string sourceName)
    {
        _totalHealedInInterval += healedAmount;
    }

    private IEnumerator ApplyHealOverTime(Character target)
    {
        var healthComponent = target.GetComponent<Health>();

        if (healthComponent != null)
        {
            float endTime = Time.time + lightDuration;
            while (Time.time < endTime)
            {
                float bonusHealFromSpiritEnergy = 0;
                if (_spiritEnergyTalent) bonusHealFromSpiritEnergy = GetSpiritEnergyBonus(target);
                float effectiveHeal = healPerTick * _accumulatedEffectiveness + bonusHealFromSpiritEnergy;

                var heal = new Heal 
                { 
                    Value = effectiveHeal,
                    DamageableSkill = this
                };
                CmdApplyHeal(heal, healthComponent.gameObject, this, name);

                _accumulatedEffectiveness += _totalHealedInInterval * effectivenessIncreasePerHeal;
                _totalHealedInInterval = 0f;

                yield return new WaitForSeconds(healInterval);
            }

            _target = null;
            ResetAccumulatedEffectiveness();
            healthComponent.HealTaked -= OnHealTaken;
        }
    }

    private IEnumerator ApplyDamageOverTime(Character target)
    {
        var healthComponent = target.GetComponent<Health>();

        if (healthComponent != null)
        {
            float endTime = Time.time + darkDuration;
            while (Time.time < endTime)
            {
                Damage damage = new Damage
                {
                    Value = Buff.Damage.GetBuffedValue(damagePerTick),
                    Type = DamageType.Magical,
                    PhysicAttackType = AttackRangeType.RangeAttack,
                    School = this.School,
                    //DamageableSkill = this,
                };

                CmdApplyDamage(damage, target.gameObject);
                yield return new WaitForSeconds(damageInterval);
            }
        }
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        characterTarget = null;

        while (_target == null)
        {
            if (Input.GetMouseButton(0))
            {
                _target = GetRaycastTarget();

                if (_target is Character character) characterTarget = character;
            }
            yield return null;
        }

        TargetInfo targetInfo = new();
        targetInfo.Targets.Add(characterTarget);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (_target == null) yield break;

        CmdPlayShootSound();

        if (isLightMode)
        {
            HandleRestorationLight();
        }
        else
        {
            HandleRestorationDark();
        }

        yield return null;
    }

    protected override void ClearData()
    {
        _target = null;
    }

    private void ResetAccumulatedEffectiveness()
    {
        _accumulatedEffectiveness = 1f;
    }

    [Command]
    private void CmdPlayShootSound()
    {
        RpcPlayShotSound();
    }

    [Command]
    private void CmdAddState(Character character, States states, float duration) => character.CharacterState.AddState(states, duration, 0, Hero.gameObject, name);

    [ClientRpc]
    private void RpcPlayShotSound()
    {
        if (_audioSource != null && audioClip != null) _audioSource.PlayOneShot(audioClip);
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        _target = (Character)targetInfo.Targets[0];
    }
} 