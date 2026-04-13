using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class FlowOfLight : Skill, IPolaritySwitchable
{
    [Header("Flow Light Settings")]
    [SerializeField] private float buffDuration = 18f;
    [SerializeField] private GameObject effectPrefabLight;
    [SerializeField] private AbilityInfo lightInfo;

    [Header("Flow Dark Settings")]
    [SerializeField] private float debuffDuration = 18f;
    [SerializeField] private GameObject effectPrefabDark;
    [SerializeField] private AbilityInfo darkInfo;

    [SerializeField] private StunMagicPassiveSkill stunMagicPassiveSkill;
    [SerializeField] private ReversePolarity _reversePolarity;

    [SyncVar(hook = nameof(OnModeChanged))] public bool isLightMode = true;
    public bool IsLightMode => isLightMode;
    public event Action OnModeChange;

    private GameObject _activeEffect;

    private bool IsAllyTarget(Character target) => target != null && target.gameObject.layer == LayerMask.NameToLayer("Allies");
    private bool IsEnemyTarget(Character target) => target != null && target.gameObject.layer == LayerMask.NameToLayer("Enemy");
    
    private float _clickRadius = 0.5f;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("FlowSpellStart");

    #region Talents

    private DestructionFillingBooster _destructionFillingBooster;
    public DestructionFillingBooster DestructionFillingBooster => _destructionFillingBooster;

    private SpiritEnergyAddBooster _spiritEnergyAddBooster;
    public SpiritEnergyAddBooster SpiritEnergyAddBooster => _spiritEnergyAddBooster;

    private SlowTalentBooster _slowTalentBooster;
    public SlowTalentBooster SlowTalentBooster => _slowTalentBooster;

    #region Aoe Talent
    private AoeTalentBooster _aoeBooster;
    public AoeTalentBooster AoeBooster => _aoeBooster;
    #endregion

    #region InstantFlashOfLight

    private InstantFlashBooster _instantFlash;

    public InstantFlashBooster InstantFlashBooster => _instantFlash;

    #endregion
    
    #region SpiritHealthOnShadow
    private bool _spiritHealthIsEnabled;
    public bool EnableSpiritHealth(bool val) => _spiritHealthIsEnabled = val;
    #endregion

    #region OverhealManaBooster

    private OverhealManaBooster _overhealMana;
    public OverhealManaBooster OverhealManaBooster => _overhealMana;

    #endregion
    
    public void SetStackingRestorationTalent(bool value) => _destructionFillingBooster.SetStackingRestoration(value);
    public void SetStackingDestructionTalent(bool value) => _destructionFillingBooster.SetStackingDestruction(value);
    public void SpiritEnergyAddTalent(bool value) => _spiritEnergyAddBooster.Enable(value);
    public void DestructionFillingTalent(bool value, float duration, float additionalTime, float chance) => _destructionFillingBooster.Enable(value, duration, additionalTime, chance);
    public void SetSlowTalent(bool value) => _slowTalentBooster.Enable(value);

    #endregion

    protected override bool IsCanCast =>
		Targeting.GetTarget()?.Character != null &&
        Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position) <= AreaInfo.Radius &&
        Targeting.NoObstacles(Targeting.GetTarget().Character.transform.position, transform.position, _obstacle) &&
        ((isLightMode && IsAllyTarget(Targeting.GetTarget()?.Character)) || (!isLightMode && IsEnemyTarget(Targeting.GetTarget()?.Character)));

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);
        UpdateMode();

        _instantFlash = new InstantFlashBooster(this, duration: 5f, chance: 10f);
        var flashSkill = Hero.Abilities.GetSkill<FlashOfLight>();
        _instantFlash.Inject(flashSkill);
        _overhealMana = new OverhealManaBooster(this, Hero);
        _aoeBooster = new AoeTalentBooster(this);
        _destructionFillingBooster = new DestructionFillingBooster(this);
        _spiritEnergyAddBooster = new SpiritEnergyAddBooster(this);
        _slowTalentBooster = new SlowTalentBooster(this);

    }

    private void OnEnable()
    {
        OnModeChange += UpdateMode;
        OnSkillCanceled += HandleSkillCanceled;
    }

    private void OnDisable()
    {
        OnModeChange -= UpdateMode;
        OnSkillCanceled -= HandleSkillCanceled;
    }

    public void FlowLightCast() => AnimStartCastCoroutine();
    public void FlowLightthEnd() => AnimCastEnded();

    public void MoveFlowLight()
    {
        _hero.Move.SetCanMove(false);
        _hero.Move.StopMoveAndAnimationMove();
    }

    public void SwitchMode()
    {
        CmdSwitchMode();
    }

    private void HandleSkillCanceled()
    {
        if (_hero != null && _hero.Move != null)
        {
            Hero.Move.SetCanMove(true);
        }
    }

    private void OnModeChanged(bool oldValue, bool newValue)
    {
        UpdateMode();
        OnModeChange?.Invoke();
    }

    private void UpdateMode()
    {
        Info.School = isLightMode ? Schools.Light : Schools.Dark;
        AbilityInfoHero = isLightMode ? lightInfo : darkInfo;
        Hero.Abilities.SkillPanelUpdate();
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        while (Targeting.GetTempTarget()?.Character == null)
        {
            if (GetMouseButton)
            {
                Vector3 clickPoint = Targeting.GetMousePoint();

                Targeting.FindTempTarget(clickPoint, _clickRadius, canTargetSelf: false);
                //_target = GetRaycastTarget(true);

                if (Targeting.GetTempTarget()?.Character != null)
                {
                    if (isLightMode && IsEnemyTarget(Targeting.GetTempTarget()?.Character) || !isLightMode && !IsEnemyTarget(Targeting.GetTempTarget()?.Character))
                    {
                        Targeting.ClearTempTarget();
                    }
                    else
                    {
                        Targeting.GetTempTarget().Character.SelectedCircle.IsActive = true;
                        _hero.Move.LookAtTransform(Targeting.GetTempTarget()?.Character.transform);
                    }
                }

            }
            yield return null;
        }
        Targeting.SetTarget(Targeting.GetTempTarget()?.Character);
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Targeting.GetTarget()?.Character);
        targetDataSavedCallback(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (Targeting.GetTarget()?.Character == null || !IsCanCast)
        {
            TryCancel();
            yield break;
        }

        TryPayCost();
        CmdSpawnEffect(gameObject, Targeting.GetTarget()?.Character.gameObject);

        float elapsed = 0f;
        float interval = 1f;
        float tickValue = 8f;

        var manaResource = Hero.TryGetResource(ResourceType.Mana);
        Vector3 initialPosition = transform.position;
        float maxMoveDistance = 0.5f;

        while (elapsed < _channelComponent.CastDuration)
        {
            if (Targeting.GetTarget().Character == null || !Targeting.GetTarget().Character.gameObject.activeSelf ||
                Input.GetMouseButtonDown(1) ||
                Vector3.Distance(transform.position, Targeting.GetTarget().Character.transform.position) > AreaInfo.Radius ||
                Vector3.Distance(transform.position, initialPosition) > maxMoveDistance ||
                (manaResource != null && manaResource.CurrentValue < 1f))
            {

                _hero.Animator.ResetTrigger(AnimTriggerCast);
                _hero.NetworkAnimator.ResetTrigger(AnimTriggerCast);

                CmdCrossFade();
                _hero.Animator.CrossFade("FlowSpellEnd", 0.1f);

                TryCancel();
                CmdDestroyEffect();
                TrySwitchSpellsOnDarkMode();
                yield break;
            }

            if (elapsed % interval < Time.deltaTime)
            {
                var currentTarget = Targeting.GetTarget()?.Character;

                if (isLightMode && IsAllyTarget(currentTarget))
                {
                    Heal heal = new Heal { Value = tickValue };
                    CmdApplyHeal(heal, currentTarget.gameObject, this, Name);
                    _instantFlash.TryApply();
                    _overhealMana.OnAnyHealTaken(currentTarget,tickValue,this);
                    
                    foreach (var tHeal in _aoeBooster?.GetHealableTargets(currentTarget,tickValue,this))
                    {
                        ApplyFromTalents(tHeal.Key,false);
                    }
                }
                else if (!isLightMode && IsEnemyTarget(currentTarget))
                {
                    Damage damage = new Damage { Value  = tickValue, Type   = Info.DamageType, School = Info.School };
                    CmdApplyDamage(damage, currentTarget?.gameObject);
                    _slowTalentBooster.TryApplySlow(currentTarget);
                    _instantFlash.TryApply();

                    foreach (var tDamage in _aoeBooster?.GetDamagebleTarget(currentTarget,tickValue,this))
                    {
                        ApplyFromTalents(tDamage.Key,false);
                    }
                }

                if (currentTarget == _hero)
                {
                    CmdApplyFromTalents(currentTarget.gameObject);
                }
                else
                {
                    ApplyFromTalents(currentTarget.gameObject,false);
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
        _slowTalentBooster.TryRemoveSlow(Targeting.GetTarget()?.Character);
        TrySwitchSpellsOnDarkMode();
        _hero.Animator.ResetTrigger(AnimTriggerCast);
        _hero.NetworkAnimator.ResetTrigger(AnimTriggerCast);
        CmdCrossFade();
        _hero.Animator.CrossFade("FlowSpellEnd", 0.1f);
        CmdDestroyEffect();
    }

    private void CmdApplyFromTalents(GameObject target)
    {
        ApplyFromTalents(target, true);
    }
    
    private void ApplyFromTalents(GameObject target,bool isAoeTarget)
    {
        if (_destructionFillingBooster.TryApply(target, isLightMode, out var dState, out var dDuration))
        {
            if(!isAoeTarget)
                target.GetComponent<CharacterState>().AddStateLogic(dState, dDuration, 0,Schools.None, gameObject, "DestructionFilling");
            else
                target.GetComponent<CharacterState>().CmdAddState(dState, dDuration, 0, gameObject, "DestructionFilling");
        }
        if (_spiritEnergyAddBooster.TryApply(target, isLightMode, buffDuration, debuffDuration, out States outState, out float outTime))
        {
            target.GetComponent<CharacterState>().AddStateLogic(outState, outTime, 0,Schools.None, gameObject, "SpiritEnergyAdd");
        }
    }

    private void TrySwitchSpellsOnDarkMode()
    {
        if (_reversePolarity != null && Hero.CharacterState.CheckForState(States.ReversePolarity))
        {
            _reversePolarity.SwitchSpells();
            _reversePolarity.RemoveReversePolarityEffect();
            _reversePolarity.SetCooldownFromSpell();
        }
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
        _hero.Move.StopLookAt();
        CmdDestroyEffect();
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0)
            Targeting.SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
    }

    [Command] private void CmdCrossFade() => _hero.Animator.CrossFade("FlowSpellEnd", 0.1f);

    [Command]
    private void CmdSwitchMode()
    {
        UpdateMode();
        isLightMode = !isLightMode;
    }

    [Command]
    private void CmdSpawnEffect(GameObject start, GameObject end)
    {
        if (effectPrefabDark == null || effectPrefabLight == null || start == null || end == null) return;

        GameObject effectInstance = null;

        if (!isLightMode) effectInstance = Instantiate(effectPrefabDark, start.transform.position, Quaternion.identity);
        else effectInstance = Instantiate(effectPrefabLight, start.transform.position, Quaternion.identity);

        //SceneManager.MoveGameObjectToScene(effectInstance, _hero.NetworkSettings.MyRoom);
        NetworkServer.Spawn(effectInstance);

        _activeEffect = effectInstance;

        RpcInitEffect(effectInstance, start, end);
    }

    [Command]
    private void CmdDestroyEffect()
    {
        if (_activeEffect != null)
        {
            NetworkServer.Destroy(_activeEffect);
            _activeEffect = null;
        }
    }
    
    [ClientRpc]
    private void RpcInitEffect(GameObject effect, GameObject start, GameObject end)
    {
        if (effect == null) return;

        FlowLightEffect[] flows = effect.GetComponentsInChildren<FlowLightEffect>(true);
        foreach (var flow in flows)
        {
            flow.Initialize(start, end);
            flow.Activate();
        }

        if (flows.Length == 0)
        {
            Debug.LogWarning("FlowLightEffect не найден ни на одном дочернем объекте эффекта: " + effect.name);
        }
    }
}
