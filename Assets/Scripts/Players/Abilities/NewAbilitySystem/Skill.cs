﻿using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
 using UnityEngine.Serialization;

 public abstract partial class Skill : NetworkBehaviour
{
    #region Variables
    #region InspectorSettings
    [Header("[Talent State]")]
    [SerializeField] protected bool _isTalentSpell = false;
    [SerializeField] protected bool _isSkillActive = true;
    [NonSerialized] public float ExtraAnimationSpeedMultiplier = 1f;

    [Header("[Skill Info]")]
    [SerializeField] private AbilityInfo _abilityInfo;
    [SerializeField] protected InfoComponent _infoComponent;
    [SerializeField] TargetingComponent _targetingComponent;
    [SerializeField] CostComponent _costComponent;
    [SerializeField] protected float _damageValue;
    [Header("[Cooldown]")]
    [SerializeField] protected bool _isSubjectToGlobalCooldownTime = true;
    [SerializeField] private CooldownComponent _cooldownComponent;
    [SerializeField] protected ChargeComponent _chargeComponent;

    [Header("[Channeling]")]
    [SerializeField] protected float _autoAttackDelay;
    [FormerlySerializedAs("_castDeley")] [SerializeField] protected float _preparingDuration;
    [SerializeField] protected ChannelComponent _channelComponent;
    [Header("[Area settings]")]
    [SerializeField] protected AreaComponent _areaComponent;
    [SerializeField] protected InformationRenderComponent _informationRenderComponent;
    [SerializeField] protected bool _disactive = false;
    [SerializeField] protected float maxCounter;
    [SerializeField] public TagComponent _tags;
    [SerializeField] private AnimationComponent _animationComponent;
    [SerializeField] private SoundComponent<Sfx_Skill> _soundComponent;
    #endregion InspectorSettings

    #region CastReduction
    
    public event Action<float> PreparingRolledBack;
    public event Action<float> ChannelingRolledBack;
    
    public event Action<float> ChannelingProgressApplied;
    protected void RaiseChannelingProgressApplied(float amount) => ChannelingProgressApplied?.Invoke(amount);
    
    protected virtual bool IsCustomChannelingActive => false;
    protected virtual bool SkipLegacyChannelingJob => false;
    
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
    //COOLDOWNS
    protected Coroutine _targetingCoroutine;
    protected Coroutine _castCoroutine;
    protected Coroutine _preparingCoroutine;
    protected Coroutine _channelingCoroutine;
    protected Coroutine _dynamicRendererJob;
    private Coroutine _targetingWrapperCoroutine;
    #endregion

    protected bool _isCanCancel = true;
    protected bool _isPlayCastAnim;

    protected bool _forceFailCastEarly;
    //test counter
    protected float _currentCounter;
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
    public bool GetMouseButton { get => _click != TypeClick.None; }
    public bool IsSubjectToGlobalCooldownTime { get => _isSubjectToGlobalCooldownTime; }
    public Character Hero { get => _hero; }
    public StatsBuff Buff => _statsBuff;
    public SkillAttributes Attributes => _skillAttributes; //TODO: Прикрепить SyncDictionary, чтобы аттрибуты синхронились по сети
    public SyncDictionary<SkillAttributeName, float> SyncAttributes { get => _syncAttributes; }
    public InfoComponent Info => _infoComponent;
    public TargetingComponent Targeting => _targetingComponent;
    public CostComponent Cost => _costComponent;
    public CooldownComponent Cooldown => _cooldownComponent;
    public ChargeComponent Charges => _chargeComponent;
    public ChannelComponent Channeling => _channelComponent;
    public AreaComponent AreaInfo => _areaComponent;
    public InformationRenderComponent Renderer => _informationRenderComponent;
    public AnimationComponent Animation => _animationComponent;
    public SoundComponent<Sfx_Skill> SoundComponent => _soundComponent;
    
    #region Sound

    protected readonly Dictionary<Sfx_Skill, AudioData> _activeLoopSounds = new();

    public virtual void PlaySound(Sfx_Skill phase)
    {
        if (SoundComponent == null) return;

        switch (phase)
        {
            case Sfx_Skill.PrepareStart:
                PlayWithLoopFallback(Sfx_Skill.PrepareStart, Sfx_Skill.PrepareLoop);
                break;

            case Sfx_Skill.CastStart:
                PlayWithLoopFallback(Sfx_Skill.CastStart, Sfx_Skill.CastLoop);
                break;

            case Sfx_Skill.PrepareEnd:
                StopLoopSound(Sfx_Skill.PrepareLoop);
                PlayOnce(Sfx_Skill.PrepareEnd);
                break;

            case Sfx_Skill.CastEnd:
                StopLoopSound(Sfx_Skill.CastLoop);
                PlayOnce(Sfx_Skill.CastEnd);
                break;

            default:
                PlayOnce(phase);
                break;
        }
    }

    private void PlayWithLoopFallback(Sfx_Skill primary, Sfx_Skill loopFallback)
    {
        if (SoundComponent[primary].Count > 0)
        {
            var asset = SoundComponent.GetRandom(primary);
            if (asset != null)
                AudioManager.Network.Play(SoundComponent.BuildAudioData(asset, Hero.netId));
            return;
        }

        if (SoundComponent[loopFallback].Count > 0)
        {
            var asset = SoundComponent.GetRandom(loopFallback);
            if (asset == null) return;

            var data = SoundComponent.BuildAudioData(asset, Hero.netId, mode: SfxPlayMode.Loop);
            _activeLoopSounds[loopFallback] = data;
            AudioManager.Network.Play(data);
        }
    }

    private void PlayOnce(Sfx_Skill phase)
    {
        if (SoundComponent[phase].Count == 0) return;

        var asset = SoundComponent.GetRandom(phase);
        if (asset == null) return;

        AudioManager.Network.Play(SoundComponent.BuildAudioData(asset, Hero.netId));
    }

    protected void StopLoopSound(Sfx_Skill loopPhase)
    {
        if (_activeLoopSounds.TryGetValue(loopPhase, out var data))
        {
            AudioManager.Network.TryStop(data);
            _activeLoopSounds.Remove(loopPhase);
        }
    }

    #endregion

    #region Scriptable Objects
    public string Name => _abilityInfo.Name;
    public string Description { get => _abilityInfo.AddingDescription; set => _abilityInfo.AddingDescription = value; }
    public string DescriptionState => _abilityInfo.DescriptionState; // test: we output a description of the state
    public string CounterSkill => _abilityInfo.Counter; // test: the counter is in the ability
    public Sprite Icon => _abilityInfo.Icon;
    public AbilityInfo AbilityInfoHero { get => _abilityInfo; set => _abilityInfo = value; }
    #endregion
    public virtual bool IsPayCostStartCooldown { get => true; }
    public SkillRenderer SkillRender => _skillRender;
    public bool IsHaveResourceOnSkill { get => CheckResourcesOnSkill(); }
    public virtual bool IsHaveResources { get => IsHaveResourceOnSkill && !Cooldown.IsActive && Charges.HasCharges; }
    public List<SkillResourceCost> SkillEnergyCosts { get => Cost.TypeOf(SkillCostType.Mandatory); }
    public List<SkillResourceCost> AdditionalSkillEnergyCosts { get => Cost.TypeOf(SkillCostType.Bonus); }
    public float PreparingDuration { get => Buff.CastSpeed.GetBuffedValue(_preparingDuration); set => _preparingDuration = value; }
    public bool IsCasting => IsExecuting;
    public float MaxCounter { get => maxCounter; set => maxCounter = value; }
    public float CurrentCounter { get => _currentCounter; set => _currentCounter = value; }
    public virtual float Damage { get => _damageValue; set => _damageValue = value; }
    public float AutoAttackDelay { get => _autoAttackDelay; }
    public ChargeCDUI LinkedChargeCDUI { get; set; }
    
    public virtual object GroupKey => GetType();
    #endregion Properties
    
    #region Events
    #region Casting Events
    public event Action<Skill> TargetingStarted;
    public event Action<Skill> TargetingSuccess;
    public event Action TargetingCanceled;
    public event Action<float> PreparingStarted;
    public event Action PreparingEnded;
    public event Action CastStarted;
    public event Action CastSuccess;
    public event Action CastFinished;
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
        if (isServer)
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
    }
    #endregion

    private void Update()
    {
        TickTimers();
        
        if (IsTargeting)
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
            return Targeting.NeedLineOfSight ? (Targeting.CanCast(Targeting.GetTarget()) && Targeting.NoObstacles())
                : Targeting.CanCast(Targeting.GetTarget());
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
        if (IsExecuting == false && _targetInfoQueue.TryPeek(out TargetInfo temp))
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

    protected virtual bool IsValidTarget(ITargetable target)
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
    protected virtual IEnumerator TargetingJob(Action<TargetInfo> targetDataSavedCallback)
    {
        yield return TargetingBehaviour(targetDataSavedCallback);
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
    

    #region LockMovementPhase

    protected enum MovementLockPhase
    {
        CastStarted,
        CastTriggered,
        CastFinished,
        CastCanceled
    }
    
    protected virtual void HandleMovementLock(MovementLockPhase phase)
    {
        if (Info.Moving == Moving.Free)
            return;

        switch (phase)
        {
            case MovementLockPhase.CastStarted:
                _hero.Move.StopMoveAndAnimationMove();
                _hero.Move.SetCanMove(false);
                break;

            case MovementLockPhase.CastTriggered:
                if (Info.Moving == Moving.UntilCast)
                    _hero.Move.SetCanMove(true);
                break;

            case MovementLockPhase.CastFinished:
            case MovementLockPhase.CastCanceled:
                _hero.Move.SetCanMove(true);
                break;
        }
    }

    #endregion
    
    #region Charges
    //[SyncVar] private int _maxCharges;
    private SyncList<double> _rechargeEndTime = new();
    public SyncList<double> RechargeTimers => _rechargeEndTime;

    public virtual bool TryUseCharge()
    {
        return Charges.TryUse();
    }

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
        else if (Charges.CooldownType == ChargeCooldownType.Infinite)
        {
            _rechargeEndTime.Add(double.MaxValue);
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

    public void CooldownReset()
    {
        Cooldown.SetReduced(0);
    }

    [Command]
    public void CmdSetTargetingAll()
    {
        Targeting.Faction = TargetFaction.All;
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
    
    #region Networked Cast Bar Events
    [Command] private void CmdBroadcastPreparingStarted(float duration) => RpcPreparingStarted(duration);
    [ClientRpc] private void RpcPreparingStarted(float duration)
    {
        if (isOwned) return;
        PreparingStarted?.Invoke(duration);
    }

    [Command] private void CmdBroadcastPreparingEnded() => RpcPreparingEnded();
    [ClientRpc] private void RpcPreparingEnded()
    {
        if (isOwned) return;
        PreparingEnded?.Invoke();
    }

    [Command] private void CmdBroadcastChannelingStarted(float duration) => RpcChannelingStarted(duration);
    [ClientRpc] private void RpcChannelingStarted(float duration)
    {
        if (isOwned) return;
        ChannelingStarted?.Invoke(duration);
    }

    [Command] private void CmdBroadcastChannelingEnded() => RpcChannelingEnded();
    [ClientRpc] private void RpcChannelingEnded()
    {
        if (isOwned) return;
        ChannelingEnded?.Invoke();
    }

    [Command] private void CmdBroadcastCanceled() => RpcCanceled();
    [ClientRpc] private void RpcCanceled()
    {
        if (isOwned) return;
        Canceled?.Invoke();
    }
    #endregion

    #region Channeling

    #region Properties
    public float ChannelingDuration => Channeling.CastDuration;
    public float ManaCostRate { get => Channeling.TickInterval; }
    public List<SkillResourceCost> ManaCostPerTick { get => Channeling.Costs; }
    #endregion

    #region Events
    public event Action<float> ChannelingStarted;
    public event Action ChannelingEnded;
    #endregion

    #region Methods
    public void InvokeChannelingStarted(float duration)
    {
        EnterChanneling();
        ChannelingStarted?.Invoke(duration);
        CmdBroadcastChannelingStarted(duration);
    }

    #endregion
    #endregion

    #region Resource Related

    protected virtual bool CheckResourcesOnSkill()
    {
        return Cost.EnoughResources();
    }

    #region ToDelete
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
            if (!Charges.IsComboPart) Charges.TryUse();
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
        return TryPayCost(Cost.TypeOf(SkillCostType.Mandatory), startCooldown);
    }
    #endregion
    #endregion

    #region Animation 
    protected abstract int AnimTriggerPrepare { get; }
    protected abstract int AnimTriggerCast { get; }
    public int AnimTriggerCastPublic => AnimTriggerCast;
    public int AnimTriggerPreparePublic => AnimTriggerPrepare;

    protected virtual void AnimCastEnded()
    {
        _isPlayCastAnim = false;
    }
    
    [Command] private void CmdBroadcastCastSuccess() => RpcCastSuccess();
    [ClientRpc] private void RpcCastSuccess()
    {
        if (isOwned) return;
        CastSuccess?.Invoke();
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
        else if (AnimTriggerPrepare != 0) //Временное решение, пока названия анимаций не перенесены в компонент
        {
            _hero.Animator.SetFloat(HashAnimPlayer.CastSpeed, GetCastSpeed());
            _hero.Animator.SetTrigger(AnimTriggerPrepare);
            _hero.NetworkAnimator.SetTrigger(AnimTriggerPrepare);
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
        //Debug.Log($"[Skill Attribute] {Hero.name} {Name} {name}: {value}", gameObject);
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
        if (IsTargeting || IsExecuting)
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

    
    [Command(requiresAuthority = false)]
    public void CmdCancelActiveSkill() => RpcCancelActiveSkill();
    
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
