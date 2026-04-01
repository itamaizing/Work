using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class FlashOfLight : Skill,IPolaritySwitchable
{
    [Header("Flash of Light Settings")]
    [SerializeField] private float _healAmount = 35f;
    [SerializeField] private float _lightRange = 4f;
    [SerializeField] private AbilityInfo lightInfo;

    [Header("Flash of Darkness Settings")]
    [SerializeField] private float _damageAmount = 35f;
    [SerializeField] private float _darkRange = 6f;
    [SerializeField] private AbilityInfo darkInfo;

    [SerializeField] private AudioClip audioClip;
    [SerializeField] private ReversePolarity _reversePolarity;

    private float _clickRadius = 0.5f;

    private bool _spiritEnergyTalent;

    //private IDamageable _target;
    //private Character _previousTarget;

    private AudioSource _audioSource;
    private bool _isCooldownTalentActive = false;
    private float _talentCooldown = 5f;
    private float _lastTalentTime = -5f;
    private float _cooldownReduction = 5f;

    public event Action OnModeChange;
    [SyncVar(hook = nameof(OnModeChanged))] public bool isLightMode = true;

    private bool IsAllyTarget(Character target) => target != null && target.gameObject.layer == LayerMask.NameToLayer("Allies");
    private bool IsEnemyTarget(Character target) => target != null && target.gameObject.layer == LayerMask.NameToLayer("Enemy");
    
    //protected override bool IsCanCast => IsCanCastCheck();

    //private bool IsCanCastCheck()
    //{
    //    if (Targeting.GetTarget()?.Character == null) return false;

    //    if (isLightMode)
    //        return (Targeting.GetTarget()?.Character is Character character &&character == Hero) ||
    //            Targeting.GetTarget()?.Character.gameObject.layer == LayerMask.NameToLayer("Allies");
    //    else
    //        return Targeting.GetTarget()?.Character.gameObject.layer == LayerMask.NameToLayer("Enemy");
    //}

    protected override int AnimTriggerCastDelay => Animator.StringToHash("Spell");
    protected override int AnimTriggerCast => 0;

    public void EnableTalentPhysicalShieldBoost(bool value)
    {
        _isCooldownTalentActive = value;
    }
    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }
    public override void LoadTargetData(TargetInfo targetInfo)
    {
        Targeting.SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
    }

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);
        UpdateMode();
    }

    private void OnEnable()
    {
        OnModeChange += UpdateMode;
    }

    private void OnDisable()
    {
        OnModeChange -= UpdateMode;
    }

    public void SwitchMode()
    {
        CmdSwitchMode();
    }

    [Command]
    private void CmdSwitchMode()
    {
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
        AreaInfo.Radius = isLightMode ? _lightRange : _darkRange;
        Info.School = isLightMode ? Schools.Light : Schools.Dark;
        AbilityInfoHero = isLightMode ? lightInfo : darkInfo;
        Targeting.Layer = isLightMode
            ? LayerMask.GetMask("Allies", "Player")
            : LayerMask.GetMask("Enemy");
        Hero.Abilities.SkillPanelUpdate();
    }
    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
       // _previousTarget = null;

        while (Targeting.GetTempTarget()?.Character == null)
        {
            if (GetMouseButton)
            {
                Vector3 clickPoint = Targeting.GetMousePoint();

                Targeting.FindTempTarget(clickPoint, _clickRadius, canTargetSelf: true);
                //_target = GetRaycastTarget(true);
                Debug.Log(Targeting.GetTempTarget()?.Object);
                if (Targeting.GetTempTarget()?.Character is Character character)
                {
                    if (Targeting.GetTempTarget()?.Character != null &&
                        (IsEnemyTarget(character) && isLightMode) || (IsAllyTarget(character) && !isLightMode))
                    {
                        Targeting.ClearTempTarget();
                        Debug.Log("Wrong");
                    }
                    else
                    {
                        Debug.Log("Right");
                        Targeting.GetTempTarget().Character.SelectedCircle.IsActive = true;
                        _hero.Move.LookAtTransform(Targeting.GetTempTarget()?.Character.transform);
                    }
                }

            }
            yield return null;
        }
        Debug.Log("Setting Target");
        Targeting.SetTarget(Targeting.GetTempTarget()?.Character);
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Targeting.GetTarget()?.Character);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        Debug.Log("CastJob");
        if (Targeting.GetTarget()?.Character == null || !IsCanCast) yield break;

        var target = Targeting.GetTarget()?.Character;
        
        if (isLightMode && IsEnemyTarget(target) || !isLightMode && !IsEnemyTarget(target))
        {
            Cooldown.ForceEnd();
            ResetCooldown();
            yield break;
        }

        /*if (TryPayCost())
        {*/

        CmdPlayShootSound();

        if (isLightMode) HandleFlashOfLight();
        else HandleFlashOfDarkness();

        if (_reversePolarity != null && Hero.CharacterState.CheckForState(States.ReversePolarity))
        {
            _reversePolarity.SwitchSpells();
            _reversePolarity.RemoveReversePolarityEffect();
            _reversePolarity.SetCooldownFromSpell();
        }
        //}

        yield return null;
    }

    private void HandleFlashOfLight()
    {
        if (_isCooldownTalentActive && Time.time - _lastTalentTime >= _talentCooldown)
        {
            ReduceCooldowns();
            _lastTalentTime = Time.time;
        }

        Heal(Targeting.GetTarget()?.Character);
    }

    private void HandleFlashOfDarkness()
    {
        Debug.Log("Damaging" + Targeting.GetTarget()?.Character.gameObject);
        Damage(Targeting.GetTarget()?.Character);
    }

    private void Heal(Character target)
    {
        var health = target.GetComponent<Health>();
        if (health == null) return;

        float bonusHealFromSpiritEnergy = 0;
        if (_spiritEnergyTalent) bonusHealFromSpiritEnergy = GetSpiritEnergyBonus(target);
        var heal = new Heal 
        {
            Value = _healAmount + bonusHealFromSpiritEnergy,
            DamageableSkill = this
        };

        CmdApplyHeal(heal, health.gameObject, this, Name);
    }

    private float GetSpiritEnergyBonus(Character target)
    {
        if (target == null) return 0f;

        var characterState = target.GetComponent<CharacterState>();
        if (characterState == null) return 0f;

        var spiritEnergyState = characterState.GetState(States.SpiritEnergy) as SpiritEnergyState;
        if (spiritEnergyState == null) return 0f;

        return spiritEnergyState.GetHealBonus();
    }

    private void Damage(Character target)
    {
        var damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(_damageAmount),
            Type = DamageType.Physical,
            PhysicAttackType = AttackRangeType.RangeAttack,
            School = this.Info.School,
        };

        CmdApplyDamage(damage, target.gameObject);
    }

    private bool IsValidTarget(Character target)
    {
        if (target == null) return false;

        if (isLightMode) return target == Hero || target.gameObject.layer == LayerMask.NameToLayer("Allies");
        else return target.gameObject.layer == LayerMask.NameToLayer("Enemy");
    }

    private void ReduceCooldowns()
    {
        foreach (var ability in Hero.Abilities.Abilities)
        {
            ability.DecreaseSetCooldown(_cooldownReduction);
            ability.Cooldown.Modify(-_cooldownReduction);
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
        if (_audioSource && audioClip)
            _audioSource.PlayOneShot(audioClip);
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
        //_target = null;
    }
}
