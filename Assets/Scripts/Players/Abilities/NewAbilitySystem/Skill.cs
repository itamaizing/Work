﻿using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class Skill : NetworkBehaviour
{
    #region Variables
    #region InspectorSettings
    [Header("Talent State")]
    [SerializeField] protected bool _isTalentSpell = false;
    [SerializeField] protected bool _isSkillActive = true;

    [Header("AbilitiesInfo")]
    [SerializeField] private AbilityInfo _abilityInfo;

    [Header("Main Settings")]
    [NonSerialized] public float ExtraAnimationSpeedMultiplier = 1f; // test
    [SerializeField] protected bool _isSubjectToGlobalCooldownTime = true;

    [SerializeField] CostComponent _costComponent;
    public CostComponent Cost => _costComponent;
    #region ResourceToDelete
    [SerializeField] protected List<SkillResourceCost> _skillEnergyCosts;
    [SerializeField] protected List<SkillResourceCost> _additionalSkillEnergyCosts;
    #endregion

    [SerializeField] protected float _cooldownTime;
    [SerializeField] private CooldownComponent _cooldownComponent;
    public CooldownComponent Cooldown => _cooldownComponent;

    [SerializeField] protected float _castDeley;
    [SerializeField] protected float _damageValue;

    [SerializeField] protected InfoComponent _infoComponent;
    public InfoComponent Info => _infoComponent;

    [SerializeField] TargetingComponent _targetingComponent;
    public TargetingComponent Targeting => _targetingComponent;
    #region TargetingToDelete
    [SerializeField] protected LayerMask _targetsLayers;
    [SerializeField] protected LayerMask _obstacle;
    #endregion

    [Header("Streaming settings")]
    [SerializeField] protected ChannelComponent _channelComponent;
    public ChannelComponent Channeling => _channelComponent;
    #region ChannelToDelete
    [SerializeField] protected float _castDuration;
    [SerializeField] protected float _manaCostRate;
    [SerializeField] protected List<SkillResourceCost> _manaCostPerTick;
    #endregion

    [Header("Charge settings")]
    [SerializeField] protected ChargeComponent _chargeComponent;
    public ChargeComponent Charges => _chargeComponent;
    #region ChargesTodDelete
    [SerializeField] private bool _isUseCharges;
    [SerializeField] protected bool _useChargesAsComboPart = false; // test
    [SerializeField] protected bool _chargesHaveSeparateCooldown;
    [SerializeField] protected int _maxCharges;
    [SerializeField] protected float _chargeCooldown;
    #endregion

    [Header("Area settings")]
    [SerializeField] protected AreaComponent _areaComponent;
    public AreaComponent AreaInfo => _areaComponent;
    [Header("Area settings")]
    [SerializeField] protected float _autoAttackDelay;

    [Header("Render settings")]
    [SerializeField] protected InformationRenderComponent _informationRenderComponent;
    public InformationRenderComponent Renderer => _informationRenderComponent;

    [Header("Availability")]
    [SerializeField] protected bool _disactive = false;
    [SerializeField] protected bool _earlyCooldown = false;
    [Header("Counter settings")]
    [SerializeField] protected float maxCounter;
    [SerializeField] public TagComponent _tags;
    [SerializeField] private AnimationComponent _animationComponent;
    public AnimationComponent Animation => _animationComponent;
    #endregion InspectorSettings

    #region CastReduction
    
    public event Action<float> CastTimeRolledBack;
    public event Action<float> CastStreamRolledBack;
    
    public event Action<float> CastStreamProgressApplied;
    protected void RaiseCastStreamProgressApplied(float amount) => CastStreamProgressApplied?.Invoke(amount);
    
    protected virtual bool IsCustomStreamActive => false;
    protected virtual bool SkipLegacyCastStreamJob => false;
    
    protected float _castTimeRollback = 0f;
    
    private const float PhysRollbackBase   = 0.20f;
    private const float MagRollbackBase    = 0.10f;
    private const float RollbackPerDamage  = 0.01f;
    #endregion
    #region Context
    protected SkillRenderer _skillRender;
    protected Character _hero;
    private StatsBuff _statsBuff = new StatsBuff();
    protected SkillAttributes _skillAttributes = new SkillAttributes();
    private readonly SyncDictionary<SkillAttributeName, float> _syncAttributes = new();
    
    #endregion
    #region Coroutines
    //COOLDOWNS
    //protected Coroutine _cooldownJob;
    protected Coroutine _rechargeJob;
    //COOLDOWNS
    protected Coroutine _prepareCoroutine;
    protected Coroutine _castCoroutine;
    protected Coroutine _castDeleyCoroutine;
    protected Coroutine _castStreamCoroutine;
    protected Coroutine _dynamicRendererJob;
    private Coroutine _actionWrapperForPreparingCoroutine;
    private Coroutine _actionWrapperForCastCoroutine;
    #endregion

    protected bool _isCanCancel = true;
    protected bool _isPlayCastAnim;
    protected bool _forceFailCastEarly;
    //test counter
    protected float _currentCounter;

    private bool _isPreparing = false;
    private bool _isCasting = false;
    private TypeClick _click;
    private bool _isAutoMode;
    #endregion Variables

    #region Properites
    public bool IsAutoMode
    {
        get
        {
            return _isAutoMode;
        }
        set
        {
            if (_isAutoMode != value)
            {
                _isAutoMode = value;
                AutoModeChanged?.Invoke(_isAutoMode);
            }
        }
    }
    public bool IsCanCancel { get => _isCanCancel; set => _isCanCancel = value; }
    public bool IsTalentSpell => _isTalentSpell;
    public virtual bool IsSkillActive
    {
        get => _isSkillActive;
        set => _isSkillActive = value;
    }
    public virtual bool Disactive
    {
        get => _disactive;
        set
        {
            if (_disactive != value)
            {
                _disactive = value;
                OnSkillStateChanged?.Invoke(_disactive);
            }
        }
    }

    public bool IsUseCharges { get => _isUseCharges; set => _isUseCharges = value; }
    public bool GetMouseButton { get => _click != TypeClick.None; }
    public bool IsSubjectToGlobalCooldownTime { get => _isSubjectToGlobalCooldownTime; }
    public Character Hero { get => _hero; }
    public StatsBuff Buff => _statsBuff;
    public SkillAttributes Attributes => _skillAttributes; //TODO: Прикрепить SyncDictionary, чтобы аттрибуты синхронились по сети
    public SyncDictionary<SkillAttributeName, float> SyncAttributes { get => _syncAttributes; }

    #region Scriptable Objects
    public string Name => _abilityInfo.Name;
    public string Description { get => _abilityInfo.AddingDescription; set => _abilityInfo.AddingDescription = value; }
    public string State => _abilityInfo.State; // test: we output the name of the state
    public string DescriptionState => _abilityInfo.DescriptionState; // test: we output a description of the state
    public string CounterSkill => _abilityInfo.Counter; // test: the counter is in the ability
    public Sprite Icon => _abilityInfo.Icon;
    public AbilityInfo AbilityInfoHero { get => _abilityInfo; set => _abilityInfo = value; }
    #endregion
    public virtual bool IsPayCostStartCooldown { get => true; }
    public bool IsPreparing => _isPreparing;
    public SkillRenderer SkillRender => _skillRender;
    public bool IsHaveResourceOnSkill { get => CheckResourcesOnSkill(); }
    public virtual bool IsHaveResources { get => IsHaveResourceOnSkill && !Cooldown.IsActive && Charges.HasCharges; }
    public List<SkillResourceCost> SkillEnergyCosts { get => _skillEnergyCosts; }
    public List<SkillResourceCost> AdditionalSkillEnergyCosts { get => _additionalSkillEnergyCosts; }
    public float CastDeley { get => Buff.CastSpeed.GetBuffedValue(_castDeley); set => _castDeley = value; }
    public bool IsCasting { get => _isCasting; protected set => _isCasting = value; }
    public float MaxCounter { get => maxCounter; set => maxCounter = value; }
    public float CurrentCounter { get => _currentCounter; set => _currentCounter = value; }
    public virtual float Damage { get => _damageValue; set => _damageValue = value; }
    public float AutoAttackDelay { get => _autoAttackDelay; }
    public ChargeCDUI LinkedChargeCDUI { get; set; }
    #endregion Properties
    
    #region Events
    #region Casting Events
    public event Action<Skill> PreparingStarted;
    public event Action<Skill> PreparingSuccess;
    public event Action PreparingCanceled;
    public event Action<float> CastDeleyStarted;
    public event Action CastDeleyEnded;
    public event Action CastStarted;
    public event Action CastSuccess;
    public event Action CastEnded;
    public event Action Canceled;
    public event Action OnSkillCanceled;
    public event Action AfterCast;
    #endregion
    public event Action<bool> AutoModeChanged;
    public event Action<float> MassageHaventMana;
    public event Action<bool> OnSkillStateChanged;
    public event Action BoostEnabled;
    public event Action BoostDisabled;
    public event Action<GameObject, Skill> OnDamageApplied;
    public event Action<GameObject, Skill> OnHealApplied;
    
    public delegate void OnBeforeApplyDamageDelegate(ref Damage damage, Skill skill,GameObject target);
    public event OnBeforeApplyDamageDelegate OnBeforeApplyDamage;

    protected void SkillAfterCastJob() => AfterCast?.Invoke();
    protected void CastEndedJob() => CastEnded?.Invoke();
    #endregion

    /// <summary>
    /// There may be a description that will be shown in the AbillityNameBox.
    /// </summary>
    public virtual string AdditionalDescription { get; }
    public void AddingDescriptionSet(bool value, string text)
    {
        AbilityInfoHero.AddingDescriptionSet(value, text);
    }

    #region Methods
    #region StartUp
    public virtual void Init(SkillRenderer render, Character hero)
    {
        _hero = hero;
        _skillRender = render;
        if (isServer)   // подписываемся до инициализации, чтобы сразу наполнить SyncDictionary
            _skillAttributes.OnAttributeModify += OnSkillAttributeChange;
        _skillAttributes.Init(hero.AttributeSystem);
        InitComponents();
    }

    public void InitComponents()
    {
        Info.Init(this);
        AreaInfo.Init(this);
        Charges.Init(this);
        Channeling.Init(this);
        Renderer.Init(this);
        Targeting.Init(this);
        Cooldown.Init(this);
        Cost.Init(this);
        Animation.Init(this);
        //CastBar, Sound
    }

    protected virtual void Awake()
    {
        if (_isUseCharges)
        {
            _currentChargers = _maxCharges;
            _remainingCooldownTimeChargers = new List<float>(new float[_maxCharges]);
            _currentChargeCooldownJob = new List<Coroutine>(new Coroutine[_maxCharges]);
        }
        else
            _currentChargers = 1;
    }
    #endregion

    private void Update()
    {
        TickTimers();
        
        if (_isPreparing)
        {
            Renderer.UpdateSmartIndicator();
        }
    }

    private void TickTimers()
    {
        double time = NetworkTime.time;

        if (time > CooldownEnd)
            Cooldown?.ForceEnd();

        for (int i = _rechargeEndTime.Count - 1; i >= 0; i--)
            if (_rechargeEndTime[i] <= time && Charges.CooldownType != ChargeCooldownType.Infinite)
                _rechargeEndTime.RemoveAt(i);
    }

    protected virtual bool IsCanCast
    {
        get
        {
            _targetInfoQueue.TryPeek(out TargetInfo temp);

            if (temp == null)
                return true;

            return Targeting.CanCast(Targeting.QueueInfoToTargetData(temp));
        }
    }


    #region Targeting
    protected IHealable _tempForHealing;
    protected Queue<TargetInfo> _targetInfoQueue = new();
    public Queue<TargetInfo> TargetInfoQueue { get => _targetInfoQueue; }

    /// <summary>
    /// Устанавливает цель из данных очереди перед кастом
    /// </summary>
    public virtual void LoadTargetData(TargetInfo targetInfo)
    {
        Targeting.SetTarget(Targeting.QueueInfoToTargetData(targetInfo));
    }

    private void LoadTargetDataForCheckCast()
    {
        if (_isCasting == false && _targetInfoQueue.TryPeek(out TargetInfo temp))
            LoadTargetData(temp);
    }

    /// <summary>
    /// Сохраняет цель в очередь в конце PrepareJob
    /// </summary>
    /// <param name="targetInfo"></param>
    private void SaveTargetData(TargetInfo targetInfo)
    {
        _targetInfoQueue.Enqueue(targetInfo);
    }


    /// <summary>
    /// Очистка данных после CastJob или при отмене
    /// </summary>
    protected virtual void ClearData()
    {
        Targeting.ClearTarget();
        Targeting.ClearTempTarget();
        AnimCastEnded();
    }

    public void ClearQueueTarget() => _targetInfoQueue.Clear();

    protected virtual bool IsValidTarget(IDamageable target)
    {
        if (target == null) return false;
        if (target is MonoBehaviour monoBehaviour) return monoBehaviour != null;

        return true;
    }
    #endregion Targeting

    /// <summary>
    /// Этап указания цели/места.
    /// PrepareJob => TargetingBehaviour => SetQueueTarget => SaveTargetData
    /// </summary>
    protected virtual IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        yield return TargetingBehaviour(targetDataSavedCallback);
    }

    
    public void HandleDirectDamageDuringCast(float damageValue, DamageType type, bool fullyAbsorbed)
    {
        if (fullyAbsorbed) return;
        if (!_isCasting) return;

        bool isStreamActive = _castStreamCoroutine != null || IsCustomStreamActive;
        bool isDelayActive  = _castDeleyCoroutine != null;

        if (!isStreamActive && !isDelayActive) return;

        float totalDuration = isStreamActive ? CastStreamDuration : _castDeley;

        float rollbackPercent = type == DamageType.Physical
            ? PhysRollbackBase + damageValue * RollbackPerDamage
            : MagRollbackBase + damageValue * RollbackPerDamage;

        float rollbackAmount = totalDuration * Mathf.Clamp01(rollbackPercent);
        _castTimeRollback += rollbackAmount;

        if (isStreamActive) CastStreamRolledBack?.Invoke(rollbackAmount);
        else CastTimeRolledBack?.Invoke(rollbackAmount);
    }
    
    /// <summary>
    ///  Каст способности.
    ///  CommitUse => LoadTargetData => CastJob
    /// </summary>
    protected abstract IEnumerator CastJob();

    #region Skill Execution Loop

    /// <summary>
    /// Основной метод задания цели. Если нужно несколько точек, доп логика и т.д. - переопределяем это
    /// По умолчанию в конце вызывает SetTarget
    /// </summary>
    protected virtual IEnumerator TargetingBehaviour(Action<TargetInfo> callbackDataSaved)
    {
        if (Targeting.SkillType == SkillType.NonTarget)
            yield break;

        TargetData targetData = null;
        while (targetData == null)
        {
            if (GetMouseButton)
                targetData = Targeting.GetTargetOrPoint();
            yield return null;
        }
        SetQueueTarget(targetData, callbackDataSaved);
    }

    /// <summary>
    /// Сохранение цели в _targetInfoQueue
    /// </summary>
    protected virtual bool SetQueueTarget(TargetData target, Action<TargetInfo> callbackDataSaved=null)
    {
        if (target == null)
            return false;
        TargetInfo targetInfo = new TargetInfo();
        switch (target.Type)
        {
            case TargetType.Object:
                targetInfo.AddTarget(target.Targetable);
                break;

            case TargetType.Point:
                targetInfo.Points.Add(target.Point);
                break;

            default:
                return false;
        }
        callbackDataSaved?.Invoke(targetInfo);
        return true;
    }

    /// <summary>
    /// Определяет какой вариант перезарядки нужен и запускет ее
    /// </summary>
    protected virtual void UseCooldownOrCharges()
    {
        if (Charges.UsesCharges && !Charges.IsComboPart)
        {
            Debug.Log("Starting Charge Cooldown");
            Charges.TryUse();
        }
        else
        {
            //if (Charges.IsComboPart)
            //    Charges.TryUse();
            Cooldown.Start();
        }
    }

    protected virtual void SpendResources()
    {
        Cost.TryPayMandatory();
    }

    /// <summary>
    /// Что делаем, если заклинание сработало
    /// </summary>
    protected virtual void CommitUse()
    {
        UseCooldownOrCharges();
        SpendResources();
    }

    public virtual float GetCastSpeed()
    {
        switch (Info.AbilityForm)
        {
            case AbilityForm.Physical:
                return Attributes.CastSpeedPhysical;
            case AbilityForm.Magic:
                return Attributes.CastSpeedMagical;
            default:
                return Attributes.CastSpeed;
        }
    }
    #endregion Skill Execution Loop


    #region Cast Related
    /// <summary>
    /// Отсюда начинается этап подготовки
    /// </summary>
    public bool TryPreparing()
    {
        if (_isPreparing == false)
        {
            foreach (var skillCost in _skillEnergyCosts)
            {
                //var currentResourceValue = _hero.Resources.Where(r => r.Type == skillCost.type);
                var resource = _hero.Resources[skillCost.type];
                resource.PhantomValueShow(skillCost.value);
            }
            _actionWrapperForPreparingCoroutine = StartCoroutine(ActionWrapperForPreparingJob());
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Главная точка входа
    /// Отсюда начинается каст
    /// </summary>
    public virtual bool TryCast()
    {
        if (_isCasting || _isPreparing)
            return false;

        LoadTargetDataForCheckCast();
        if (IsHaveResources && IsCanCast && _isCasting == false && Targeting.NoObstacles() && Hero.IsDead == false)
        {
            _isCasting = true;
            //TryPayCost(IsPayCostStartCooldown); //moved to ActionWrapper

            if (_targetInfoQueue.Count > 0)
            {

                var targetInfo = _targetInfoQueue.Dequeue();

                LoadTargetData(targetInfo);

                if (targetInfo.GetTargets().Count > 0)
                {
                    if (targetInfo.GetTargets()[0] is Character target)
                        _hero.Move.LookAtTransform(target.transform);
                }

                if (targetInfo.Points.Count > 0)
                {
                    var point = (Vector3)targetInfo.Points[0];
                    _hero.Move.LookAtPosition(point);
                }
            }

            _actionWrapperForCastCoroutine = StartCoroutine(ActionWrapperForCastingJob());

            return true;
        }
        else return false;
    }

    public bool TryCast(TargetInfo targetInfo)
    {
        if (_isCasting || _isPreparing)
            return false;

        LoadTargetDataForCheckCast();
        if (IsHaveResources && _isCasting == false && Targeting.NoObstacles() && Hero.IsDead == false)
        {
            LoadTargetData(targetInfo);

            if (IsCanCast)
            {
                _isCasting = true;
                //TryPayCost(IsPayCostStartCooldown);

                _actionWrapperForCastCoroutine = StartCoroutine(ActionWrapperForCastingJob());

                if (_targetInfoQueue.Count > 0)
                {
                    if (targetInfo.GetTargets().Count > 0)
                    {
                        var target = (Character)targetInfo.GetTargets()[0];
                        _hero.Move.LookAtTransform(target.transform);
                    }

                    if (targetInfo.Points.Count > 0)
                    {
                        var point = (Vector3)targetInfo.Points[0];
                        _hero.Move.LookAtPosition(point);
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    public bool TryCancel(bool forceCancel = false)
    {
        foreach (var skillCost in _skillEnergyCosts)
        {
            //var currentResourceValue = _hero.Resources.Where(r => r.Type == skillCost.type);
            var resource = _hero.Resources[skillCost.type];
            resource.PhantomValueShow(0);
            //resourse.
        }

        if (forceCancel || _isCanCancel)
        {
            Hero.Abilities.NotifySkillIsPreparing(this, false);
            Canceled?.Invoke();
            _hero.Move.SetCanMove(true);
            ClearData();
            _isPlayCastAnim = false;

            if (_dynamicRendererJob != null)
            {
                StopCoroutine(_dynamicRendererJob);
            }
            CancelCoroutine(_castCoroutine);

            if (_actionWrapperForCastCoroutine != null)
            {
                StopCoroutine(_actionWrapperForCastCoroutine);
                CancelCoroutine(_castCoroutine);
                _actionWrapperForCastCoroutine = null;
                _isCasting = false;
                ClearData();

                try { CastEnded?.Invoke(); }
                catch (Exception ex) { Debug.LogError($"[Skill:{Name}] CastEnded subscriber threw: {ex}"); }
            }

            CancelCoroutine(_castDeleyCoroutine);
            CancelCoroutine(_castStreamCoroutine);
            
            _castTimeRollback = 0f;

            if (_actionWrapperForPreparingCoroutine != null)
            {
                StopCoroutine(_actionWrapperForPreparingCoroutine);
                CancelCoroutine(_prepareCoroutine);
                _actionWrapperForPreparingCoroutine = null;
                _isPreparing = false;
                Renderer.HideSmartIndicator();

                PreparingCanceled?.Invoke();

                UnSubscribeClickEvents();
                OnClickCanceled();
            }

            //_tempTargetbase = null; => Targeting.ClearTemporary()?
            Targeting.ClearTempTarget();

            CancelAnim();
            try { OnSkillCanceled?.Invoke(); }
            catch (Exception ex) { Debug.LogError($"[Skill:{Name}] OnSkillCanceled subscriber threw: {ex}"); }


            return true;
        }
        else
        {
            Hero.Abilities.NotifySkillIsPreparing(this, false);
            return false;
        }
    }

    private IEnumerator ActionWrapperForPreparingJob()
    {
        PreparingStarted?.Invoke(this);
        _isPreparing = true;
        //ClearData();
        Renderer.ShowSmartIndicator();

        if (_informationRenderComponent.IsDynamicRenderer)
        {
            StartDynamicRenderer();
        }

        SubscribeClickEvents();
        _skillRender.SetPrepareCursor();

        yield return _prepareCoroutine = StartCoroutine(PrepareJob(SaveTargetData));

        UnSubscribeClickEvents();

        OnClickCanceled();

        //test
        if (_targetInfoQueue.TryPeek(out TargetInfo info))
        {
            if (info.GetTargets().Count > 0)
            {
                if (info.GetTargets()[0] is Character targetCharacter && targetCharacter != _hero)
                {
                    targetCharacter.UIComponent.CircleSelect1.IsActive = false;
                }
            }
        }

        PreparingSuccess?.Invoke(this);
        Targeting.ClearTempTarget();
        _isPreparing = false;
        Renderer.HideSmartIndicator();

        _prepareCoroutine = null;
    }

    private void CancelCastEarly()
    {
        _isCasting = false;
        _isPlayCastAnim = false;

        CancelAnim();

        _hero.Move.StopLookAt();
        if (_hero != null && _hero.Move != null)
        {
            _hero.Move.SetCanMove(true);
        }

        ClearData();

        if (_hero != null && _hero.Abilities != null)
        {
            _hero.Abilities.NotifySkillIsPreparing(this, false);

            if (_hero.Abilities.SkillQueue != null)
            {
                _hero.Abilities.SkillQueue.TryCancel();
            }
        }

        CastEnded?.Invoke();
        OnSkillCanceled?.Invoke();
        Canceled?.Invoke();

        if (_hero != null && _hero.UIComponent != null)
        {
            _hero.UIComponent.Miss();
        }
    }

    private IEnumerator ActionWrapperForCastingJob()
    {
        if (_forceFailCastEarly)
        {
            _forceFailCastEarly = false;
            CancelCastEarly();
            yield break;
        }

        Hero.Abilities.NotifySkillPrepared(this);
        Hero.Abilities.NotifySkillIsPreparing(this, true); 
        CastStarted?.Invoke();
        _isCasting = true;

        bool noCast = Hero.Abilities.TryConsumeNoCast();


        if (!noCast && CastDeley > 0)
            yield return StartCastDeleyCoroutine();

        if (_forceFailCastEarly)
        {
            _forceFailCastEarly = false;
            CancelCastEarly();
            yield break;
        }

        if (!noCast && AnimTriggerCast != 0)
        {
            _isPlayCastAnim = true;
            //_isWaitingForCastCoroutine = true;

            PlayCastAnim();

            if (_forceFailCastEarly)
            {
                _forceFailCastEarly = false;
                _isCasting = false;
                _isPlayCastAnim = false;

                CancelAnim();
                _hero.Move.StopLookAt();
                Hero.Move.SetCanMove(true);

                ClearData();
                CastEnded?.Invoke();
                OnSkillCanceled?.Invoke();
                Canceled?.Invoke();
                _actionWrapperForCastCoroutine = null;
                Hero.UIComponent.Miss();
                yield break;
            }

            while (_isPlayCastAnim)
            {
                if (_forceFailCastEarly)
                {
                    _forceFailCastEarly = false;
                    CancelCastEarly();
                    _actionWrapperForCastCoroutine = null;
                    yield break;
                }
                
                if (Targeting.ForDamage?.Damageable != null && !IsValidTarget(Targeting.ForDamage?.Damageable))
                {
                    _isCanCancel = true;
                    _hero.Move.SetCanMove(true);

                    TryCancel(true);
                    yield break;
                }

                if (!IsCanCast)
                {
                    TryCancel(true);
                    yield break;
                }
                //*/
                yield return null;
            }

            //_isWaitingForCastCoroutine = false;
        }

        else
        {
            if (_forceFailCastEarly)
            {
                _forceFailCastEarly = false;
                CancelCastEarly();
                yield break;
            }

            CancelAnim();

            _castCoroutine = StartCoroutine(CastJob());
            if (_castDuration > 0 && !SkipLegacyCastStreamJob) _castStreamCoroutine = StartCoroutine(CastStreamJob());
            yield return _castCoroutine;
        }

        CancelAnim();

        CommitUse();
        CastSuccess?.Invoke();
        CastEnded?.Invoke();
        _isCasting = false;

        Hero.Abilities.NotifySkillIsPreparing(this, false); 
        
        ClearData();

        /// test
        if (Targeting.ForDamage != null && Targeting.ForDamage.Character != null)
        {
            Targeting.ForDamage.Character.SelectedCircle.IsActive = false;
            Targeting.ForDamage.Character.SelectedCircle.SwitchSelectCircle(false);
        }

        _hero.Move.StopLookAt();
        if (!_isAutoMode) _hero.Move.SetCanMove(true);

        _castCoroutine = null;
    }
    
    #region CastDelay
    protected Coroutine StartCastDeleyCoroutine(float time = float.MinValue)
    {
        if (time == float.MinValue)
            time = CastDeley;

        _castDeleyCoroutine = StartCoroutine(CastDeleyJob(time));
        return _castDeleyCoroutine;
    }

    private IEnumerator CastDeleyJob(float delayTime)
    {
        CastDeleyStarted?.Invoke(delayTime);
        PlayPrepareAnim();
        float time = 0;

        while (time < delayTime)
        {
            if (_forceFailCastEarly)
            {
                _forceFailCastEarly = false;
                CancelCastEarly();
                yield break;
            }
            
            if (Targeting.NoObstacles() == false)
                TryCancel(true);

            if (_castTimeRollback > 0f)
            {
                time = Mathf.Max(0f, time - _castTimeRollback);
                _castTimeRollback = 0f;

                float remaining = delayTime - time;
                Animation.SyncSpeedToRemaining(Animation.ActiveClipRawLength, remaining);
            }

            time += Time.deltaTime;
            yield return null;
        }
        _castDeleyCoroutine = null;
        CastDeleyEnded?.Invoke();
    }
    #endregion CastDelay
    #endregion

    #region Charges
    // Пока не вырезал, есть скиллы завязанные на ручном управлении зарядами
    // Для переписывания, добавил в новую систему тип Infinite (не тикающие)
    #region Old
    protected int _currentChargers;
    private List<float> _remainingCooldownTimeChargers = new();
    private List<Coroutine> _currentChargeCooldownJob;

    #region ChargeRelatedProperties
    public int Chargers { get => _currentChargers; protected set { _currentChargers = value; CurrentChargeChanged?.Invoke(_currentChargers); } }
    public List<float> RemainingCooldownTimeCharge => _remainingCooldownTimeChargers;
    #endregion

    #region Charge Events
    public event Action<int> CurrentChargeChanged;
    public event Action<float> ChargeStartCooldown;
    public event Action<int> ChargeCooldownEnded;
    public event Action MassageHaventCharge;
    #endregion

    #region Methods
    public virtual bool TryUseCharge()
    {
        if (_isUseCharges == false)
            return true;

        Charges.TryUse();

        if (_currentChargers > 0)
        {
            _currentChargers--;
            CurrentChargeChanged?.Invoke(_currentChargers);

            if (_rechargeJob == null && _chargesHaveSeparateCooldown == false)
            {
                _rechargeJob = StartCoroutine(RechargeCoroutine());
            }
            else if (_rechargeJob == null && _chargesHaveSeparateCooldown)
            {
                for (int i = 0; i < _maxCharges; i++)
                {
                    if (_remainingCooldownTimeChargers[i] <= 0)
                    {
                        _currentChargeCooldownJob[i] = StartCoroutine(RechargeOneChargeCoroutine(i, Charges.CooldownTime));

                        ChargeStartCooldown?.Invoke(Charges.CooldownTime);
                        break;
                    }
                }
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    private IEnumerator RechargeOneChargeCoroutine(int chargeIndex, float time)
    {
        _remainingCooldownTimeChargers[chargeIndex] = time;

        while (_remainingCooldownTimeChargers[chargeIndex] > 0)
        {
            _remainingCooldownTimeChargers[chargeIndex] -= Time.deltaTime;

            yield return null;
        }

        if (_currentChargers < _maxCharges)
        {
            _currentChargers++;
            CurrentChargeChanged?.Invoke(_currentChargers);
        }
    }

    protected virtual IEnumerator RechargeCoroutine()
    {
        while (_currentChargers < _maxCharges)
        {
            ChargeStartCooldown?.Invoke(Charges.CooldownTime);
            float time = 0;
            while (time < Charges.CooldownTime)
            {
                time += Time.deltaTime;
                yield return null;
            }
            if (_currentChargers < _maxCharges)
            {
                _currentChargers++;
                CurrentChargeChanged?.Invoke(_currentChargers);
            }
        }
        _rechargeJob = null;
    }
    #endregion Coroutines
    #endregion Old

    #region NewSystem
    //[SyncVar] private int _maxCharges;
    private SyncList<double> _rechargeEndTime = new();
    public SyncList<double> RechargeTimers => _rechargeEndTime;


    [Command]
    public void CmdStartRecharge(float duration)
    {
        if (Charges.CooldownType == ChargeCooldownType.Independant)
        {
            _rechargeEndTime.Add(NetworkTime.time + duration);
        }
        else if (Charges.CooldownType == ChargeCooldownType.Sequential)
        {
            var endTime = NetworkTime.time + duration;
            if (_rechargeEndTime.Count > 0)
            {
                endTime = _rechargeEndTime.Last() + duration;
            }
            _rechargeEndTime.Add(endTime);
        }
    }

    [Command]
    public void CmdModifyRechargeTime(float time, bool tickAll)
    {
        if (tickAll)
        {
            for (int i = _rechargeEndTime.Count - 1; i >= 0; i--)
            {
                _rechargeEndTime[i] += time;
                if (_rechargeEndTime[i] <= NetworkTime.time)
                    _rechargeEndTime.RemoveAt(i);
            }
        }
        else if (_rechargeEndTime.Count > 0)
        {
            _rechargeEndTime[0] -= time; //Первый заряд всегда самый старый, если мы не приколисты

            if (_rechargeEndTime[0] <= NetworkTime.time) //В теории можно излишек перезарядки снимать с кд след. заряда, но как будто не стоит
                _rechargeEndTime.RemoveAt(0);
        }
    }

    [Command]
    public void CmdEndRecharge(int index)
    {
        _rechargeEndTime.RemoveAt(index);
    }
    #endregion
    #endregion CHARGES

    #region Cooldown
    [SyncVar(hook = nameof(OnCooldownChanged))] private double _cooldownEndTime = 0;
    public double CooldownEnd => _cooldownEndTime;
    private void OnCooldownChanged(double oldValue, double newValue)
    {
        Cooldown?.OnServerCooldownChanged(oldValue, newValue);
    }

    [Command]
    public void CmdCooldownStart(float duration)
    {
        _cooldownEndTime = NetworkTime.time + duration;
        if (duration >= 1) // чтобы не спамило ГКД/скиллы без КД
            Debug.Log("CD STARTED " + duration);
    }

    [Command]
    public void CmdCooldownModify(double delta)
    {
        if (_cooldownEndTime <= NetworkTime.time)
            return;

        _cooldownEndTime += delta;

        if (_cooldownEndTime <= NetworkTime.time)
        {
            _cooldownEndTime = NetworkTime.time;
            Cooldown?.ForceEnd();
        }
    }

    [Command]
    public void CmdCooldownEnd()
    {
        _cooldownEndTime = NetworkTime.time;
    }

    [Server]
    public void ServerResetCooldownOnly()
    {
        _cooldownEndTime = NetworkTime.time;

        //_remainingCooldownTime = 0;
        //CooldownEnded?.Invoke();
        Debug.Log($"_cooldownEndTime");
    }
    #endregion

    #region Channeling

    #region Properties
    public float CastStreamDuration => Channeling.CastDuration; // Ctrl+R
    public float ManaCostRate { get => _manaCostRate; }
    public List<SkillResourceCost> ManaCostPerTick { get => Channeling.Costs; }
    #endregion

    #region Events
    public event Action<float> CastStreamStarted;
    public event Action CastStreamEnded;
    #endregion

    #region Methods
    public void InvokeCastStreamStarted(float duration)
    {
        CastStreamStarted?.Invoke(duration);
    }
    private IEnumerator CastStreamJob()
    {
        CastStreamStarted?.Invoke(CastStreamDuration);
        float time = 0;

        while (time < CastStreamDuration)
        {
            if (_castTimeRollback > 0f)
            {
                time += _castTimeRollback;
                _castTimeRollback = 0f;

                float remaining = CastStreamDuration - time;
                Animation.SyncSpeedToRemaining(Animation.ActiveClipRawLength, remaining);
            }
            
            time += _manaCostRate;

            foreach (var skillCost in _manaCostPerTick)
            {
                var currentResourceValue = _hero.Resources[skillCost.type].CurrentValue;

                if (currentResourceValue < Buff.ManaCost.GetBuffedValue(skillCost.value))
                {
                    TryCancel(true);
                }
                else
                {
                    var resource = _hero.Resources[skillCost.type];
                    resource.CmdUse(Buff.ManaCost.GetBuffedValue(skillCost.value));
                }
            }
            yield return new WaitForSeconds(_manaCostRate);
        }
        _castStreamCoroutine = null;
        CastStreamEnded?.Invoke();
    }

    #endregion
    #endregion

    #region Resource Related
    protected virtual bool CheckResourcesOnSkill()
    {
        return Cost.EnoughResources();
    }

    protected virtual bool TryPayCost(List<SkillResourceCost> skillEnergyCosts, bool startCooldown = true)
    {
        if (IsHaveResourceOnSkill)
        {
            foreach (var skillCost in skillEnergyCosts)
            {
                var resource = _hero.Resources[skillCost.type];
                //resource.CmdUse(Buff.ManaCost.GetBuffedValue(skillCost.value)); // moved to ActionWrapper
            }

            if (startCooldown)
            {
                Cooldown.Start();
            }
            if (!Charges.IsComboPart) TryUseCharge();
            return true;
        }
        else
        {
            return false;
        }
    }

    protected virtual bool TryPayCost(bool startCooldown = true)
    {
        if (_hero.Abilities.TryConsumeNextSkillFree()) return true;
        return TryPayCost(_skillEnergyCosts, startCooldown);
    }
    #endregion

    #region Animation 
    protected abstract int AnimTriggerCastDelay { get; }
    protected abstract int AnimTriggerCast { get; }
    public int AnimTriggerCastPublic => AnimTriggerCast;


    [ClientCallback]
    protected void AnimStartCastCoroutine()
    {
        _castCoroutine = StartCoroutine(CastJob());
        if (_castDuration > 0) _castStreamCoroutine = StartCoroutine(CastStreamJob());
    }

    protected virtual void AnimCastEnded()
    {
        _isPlayCastAnim = false;
    }

    protected virtual void PlayCastAnim()
    {
        _isPlayCastAnim = true;
        if (Animation.CastTriggers.Count > 0)
        {
            Animation.PlayCasting();
        }
        else if (AnimTriggerCast != 0) //Временное решение, пока названия анимаций не перенесены в компонент
        {
            _hero.Animator.SetFloat(HashAnimPlayer.CastSpeed, GetCastSpeed());
            _hero.Animator.SetTrigger(AnimTriggerCast);
            _hero.NetworkAnimator.SetTrigger(AnimTriggerCast);
        }
    }

    protected virtual void PlayPrepareAnim()
    {
        if (Animation.PrepareTriggers.Count > 0)
        {
            Animation.PlayPreparing();
        }
        else if (AnimTriggerCastDelay != 0) //Временное решение, пока названия анимаций не перенесены в компонент
        {
            _hero.Animator.SetFloat(HashAnimPlayer.CastSpeed, GetCastSpeed());
            _hero.Animator.SetTrigger(AnimTriggerCastDelay);
            _hero.NetworkAnimator.SetTrigger(AnimTriggerCastDelay);
        }
    }

    protected virtual void CancelAnim()
    {
        Animation.Cancel();
    }
    #endregion
    
    #region Boost
    protected virtual void SkillEnableBoostLogic() { }

    protected virtual void SkillDisableBoostLogic() { }

    public void EnableSkillBoost()
    {
        SkillEnableBoostLogic();
        BoostEnabled?.Invoke();
    }

    public void DisableSkillBoost()
    {
        SkillDisableBoostLogic();
        BoostDisabled?.Invoke();
    }
    #endregion

    #region Custom Radius Rendering

    public virtual void StartCustomDraw()
    {

    }
    public virtual void StopCustomDraw()
    {
        
    }
    public virtual IEnumerator CustomDrawJob(float time = 0.2f)
    {
        yield return null; //new WaitForSeconds(time);
    }

    private void StartDynamicRenderer()
    {
        _dynamicRendererJob = StartCoroutine(CustomDrawJob());
    }

    public void StopDynamicRender()
    {
        if (_dynamicRendererJob != null)
            StopCoroutine(_dynamicRendererJob);
    }
    #endregion

    protected void CancelCoroutine(Coroutine coroutine)
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
        }
    }

    #region ScoreBoard?
    private void AddAssist(Character character)
    {
        Hero.AssystCounter++;
    }

    private void AddAssist()
    {
        Hero.AssystCounter++;
    }

    private void AddKill(Character character)
    {
        Hero.KillCounter++;
    }
    #endregion

    #region Server-side

    [Server]
    private void OnSkillAttributeChange(string name, float value)
    {
        Debug.Log($"[Skill Attribute] {Name} {name}: {value}");
        if (!Enum.TryParse<SkillAttributeName>(name, out SkillAttributeName attr))
            return;
        if (_syncAttributes.Keys.Contains(attr))
            _syncAttributes[attr] = value;
        else
            _syncAttributes.Add(attr, value);
    }

    [ClientRpc]
    public void RpcResetSkillState()
    {
        ResetSkillState();
    }


    [ClientRpc]
    public void RpcCancelActiveSkill()
    {
        if (_isPreparing || _isCasting)
        {
            TryCancel(true);
        }
    }

    [ClientRpc]
    private void RpcForceFailCastJobOnce()
    {
        _forceFailCastEarly = true;
    }

    [Command] public void CmdForceFailCastJobOnce() => RpcForceFailCastJobOnce();

    [Command]
    public void CmdCancelActiveSkill() => RpcCancelActiveSkill();

    public void ResetSkillState()
    {
        //ResetCooldownStateOnly();

        if (_castDeleyCoroutine != null)
        {
            StopCoroutine(_castDeleyCoroutine);
            _castDeleyCoroutine = null;
        }
        CastDeleyEnded?.Invoke();

        _isPreparing = false;
        _isCasting = false;
        _isAutoMode = false;

        if (_isUseCharges)
        {
            _currentChargers = _maxCharges;
            CurrentChargeChanged?.Invoke(_currentChargers);
        }

        if (_castStreamCoroutine != null)
        {
            StopCoroutine(_castStreamCoroutine);
            _castStreamCoroutine = null;
        }
        CastStreamEnded?.Invoke();

        CancelCoroutine(_castCoroutine);
        CancelCoroutine(_actionWrapperForPreparingCoroutine);
        CancelCoroutine(_actionWrapperForCastCoroutine);
        ClearData();
    }


    public void ApplyDamage(Damage damage, GameObject target)
    {
        OnBeforeApplyDamage?.Invoke(ref damage, this, target);
        var damageable = target != null ? target.GetComponent<IDamageable>() : null;
        Character targetCharacter = target != null ? target.GetComponent<Character>() : null;

        if (targetCharacter)
        {
            if (targetCharacter.IsDead)
            {
                return;
            }
        }

        if (damageable != null)
        {
            damageable.TryTakeDamage(ref damage, this);
            OnDamagedApplied(target);

            //_hero.DamageTracker.AddDamage(damage, target, isServerRequest: isServer);
            //_hero.DamageGet(damage, target);
            TryCountGettedDamage(damage);
        }

        else
        {
            Debug.LogWarning($"[Skill] Target {target?.name} is not damageable or null");
        }

        _hero.DamageTracker.AddDamage(damage, target, isServerRequest: isServer);
        _hero.DamageGet(damage, target);

    }
    [ClientRpc]
    private void OnDamagedApplied(GameObject target)
    {
        OnDamageApplied?.Invoke(target, this);
    }

    private void TryCountGettedDamage(Damage damage)
    {
        if (_hero is MinionComponent minion)
        {
            if (minion)
            {
                if (minion.CharacterParent != null)
                {
                    minion.CharacterParent.IncreaseGettedDamage(damage);
                }
                else
                {
                    Debug.LogError("PARENT IS NULL");
                }
            }
        }
        else
        {
            _hero.IncreaseGettedDamage(damage);
        }
    }

    private void OnTargetDied(GameObject target)
    {
        var character = target != null ? target.GetComponent<Character>() : null;

        if (character)
        {
            if (character.IsDead)
            {
                AddKill(character);
            }
        }
    }

    public void CmdApplyDamage(Damage damage, GameObject target)
    {
        _hero.DamageGet(damage, target);
        CmdApplyDamageLogic(damage, target);
    }

    [Command]
    private void CmdApplyDamageLogic(Damage damage, GameObject target)
    {
        if (target == null) return;

        if (Targeting.ForDamage == null || Targeting.ForDamage?.Transform != target.transform)
        {
            Targeting.ForDamage = new TargetData(target);
        }

        if (target == null)
        {
            Debug.LogError("[CmdApplyDamageLogic] Target is null, skipping");
            return;
        }

        ApplyDamage(damage, target);
    }

    public void ApplyHeal(Heal heal, GameObject hp, Skill skill, string sourceName)
    {
        Debug.Log(hp);
        hp.GetComponent<IHealable>().Heal(ref heal, sourceName, skill);
        Hero.DamageTracker.AddHeal(heal, isServerRequest: isServer);
    }

    [Command]
    public void CmdApplyHeal(Heal heal, GameObject hp, Skill skill, string sourceName)
    {
        if (Targeting.ForDamage == null || Targeting.ForDamage?.Transform != hp.transform)
        {
            Targeting.ForDamage = new TargetData(hp);
            _tempForHealing = hp.GetComponent<IHealable>();
        }
        if (_tempForHealing != null)
        {
            ApplyHeal(heal, hp, skill, sourceName);
            OnHealApply(hp);
        }
    }

    [ClientRpc]
    private void OnHealApply(GameObject target)
    {
        OnHealApplied?.Invoke(target, this);
    }
    #endregion Server-side

    public void AfterCastJob()
    {
        CmdSkillAfterCastJob();
        SkillAfterCastJob();
    }

    [Command] private void CmdSkillAfterCastJob() => SkillAfterCastJob();

    #region OnClicks
    private void OnClick()
    {
        _click = TypeClick.LMB;
    }

    private void OnClickCanceled()
    {
        _click = TypeClick.None;
    }

    private void OnShiftClick()
    {
        _click = TypeClick.ShiftLMB;
    }

    private void OnCtrlClick()
    {
        _click = TypeClick.CtrlLMB;
    }

    private void OnSpaceClick()
    {
        _click = TypeClick.SpaceLMB;
    }
    #endregion

    private void SubscribeClickEvents()
    {
        InputHandler.OnClick += OnClick;
        InputHandler.OnShiftLeftMouse += OnShiftClick;
        InputHandler.OnSwitchAutoMode += OnCtrlClick;
        InputHandler.OnSpacetLeftMouse += OnSpaceClick;

        //cancelled

        InputHandler.OnClickCanceled += OnClickCanceled;
        InputHandler.OnShiftLeftMouseCanceled += OnClickCanceled;
        InputHandler.OnSwitchAutoModeCanceled += OnClickCanceled;
        InputHandler.OnSpacetLeftMouseCanceled += OnClickCanceled;

    }

    private void UnSubscribeClickEvents()
    {
        InputHandler.OnClick -= OnClick;
        InputHandler.OnShiftLeftMouse -= OnShiftClick;
        InputHandler.OnSwitchAutoMode -= OnCtrlClick;
        InputHandler.OnSpacetLeftMouse -= OnSpaceClick;

        //cancelled

        InputHandler.OnClickCanceled -= OnClickCanceled;
        InputHandler.OnShiftLeftMouseCanceled -= OnClickCanceled;
        InputHandler.OnSwitchAutoModeCanceled -= OnClickCanceled;
        InputHandler.OnSpacetLeftMouseCanceled -= OnClickCanceled;

    }
    #endregion
}
