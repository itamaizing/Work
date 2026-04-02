﻿using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public class SkillEnergyCost
{
    public ResourceType resourceType;
    public float resourceCost;

    public void ModifyResourceCost(float multiplier)
    {
        resourceCost *= multiplier;
    }

    public void ModifyResourceCost1(float multiplier)
    {
        resourceCost /= multiplier;
    }

    public void ResetResourceCost(float baseCost)
    {
        resourceCost = baseCost;
    }
}

/*
public class TargetToShot
{
    public Vector3 Position;
    public ITargetable targetable;

    public Character character;
    public IDamageable damageable;
    public bool isCharater = false;
}*/


public abstract class Skill : NetworkBehaviour
{
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
    [SerializeField] protected List<SkillEnergyCost> _skillEnergyCosts;
    [SerializeField] protected List<SkillEnergyCost> _additionalSkillEnergyCosts;
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
    [SerializeField] protected List<SkillEnergyCost> _manaCostPerTick;
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
    #endregion InspectorSettings

    #region Context
    protected SkillRenderer _skillRender;
    protected Character _hero;
    private StatsBuff _statsBuff = new StatsBuff();
    protected SkillAttributes _skillAttributes = new SkillAttributes();
    #endregion
    protected bool _isCanCancel = true;

    #region Coroutines
    protected Coroutine _prepareCoroutine;
    protected Coroutine _castCoroutine;
    //COOLDOWNS
    protected Coroutine _cooldownJob;
    protected Coroutine _rechargeJob;
    //COOLDOWNS
    protected Coroutine _castDeleyCoroutine;
    protected Coroutine _castStreamCoroutine;
    protected Coroutine _dynamicRendererJob;
    #endregion
    protected bool _isPlayCastAnim;
    protected bool _forceFailCastEarly;


    //Charges
    #region Charges
    protected int _currentChargers;
    private List<float> _remainingCooldownTimeChargers = new();
    private List<Coroutine> _currentChargeCooldownJob;

    #region ChargeRelatedProperties
    public bool IsUseCharges => Charges.UsesCharges;
    public int MaxChargers => Charges.MaxCharges; //Ctrl+R
    public int Chargers { get => _currentChargers; protected set { _currentChargers = value; CurrentChargeChanged?.Invoke(_currentChargers); } }
    public bool IsHaveCharge => Charges.HasCharges;
    public float ChargeCooldown => Charges.BaseCooldown;
    public List<float> RemainingCooldownTimeCharge => Charges.ActiveCooldowns;
    #endregion

    #region Charge Events
    public event Action<int> CurrentChargeChanged;
    public event Action<float> ChargeStartCooldown;
    public event Action<int> ChargeCooldownEnded;
    public event Action MassageHaventCharge;
    #endregion

    #region Methods
    public void ResetCurrentChargeCooldown(int index)
    {
        if (!_isUseCharges || !_chargesHaveSeparateCooldown) return;
        if (_currentChargeCooldownJob[index] != null)
        {
            StopCoroutine(_currentChargeCooldownJob[index]);
            _currentChargeCooldownJob[index] = null;
        }

        _remainingCooldownTimeChargers[index] = 0f;

        LinkedChargeCDUI?.RemoveChargeCD(index);

        _currentChargers = Mathf.Min(_currentChargers + 1, _maxCharges);
        CurrentChargeChanged?.Invoke(_currentChargers);
        ChargeCooldownEnded?.Invoke(index);
    }
    public void AddMaxChargeCount()
    {
        bool isRecharging = (_currentChargers < _maxCharges);

        _maxCharges += 1;

        _remainingCooldownTimeChargers.Add(0);
        if (!isRecharging)
            _currentChargers += 1;

        CurrentChargeChanged?.Invoke(_currentChargers);
    }



    public void ReductionCooldownForAllCharges(float reductionTime, float reductionPercentage = 0)
    {
        for (int i = 0; i < RemainingCooldownTimeCharge.Count; i++)
        {
            var time = _remainingCooldownTimeChargers[i] - reductionTime - (_remainingCooldownTimeChargers[i] * reductionPercentage);
            ReductionCooldownForCharge(i, time);
        }
    }

    public void ReductionCooldownCharges(float reductionTime)
    {
        float time;
        for (int i = 0; i < RemainingCooldownTimeCharge.Count; i++)
        {
            time = _remainingCooldownTimeChargers[i] - reductionTime;

            if (time <= 0)
            {
                ReductionCooldownForCharge(i, reductionTime);
                reductionTime = reductionTime - _remainingCooldownTimeChargers[i];
            }
            else
            {
                ReductionCooldownForCharge(i, reductionTime);
                break;
            }
        }
    }

    public void DeductMaxChargeCount()
    {
        if (_maxCharges - 1 > 0)
        {
            int lastIndex = _maxCharges - 1;

            _remainingCooldownTimeChargers.RemoveAt(lastIndex);

            _maxCharges -= 1;
            if (_currentChargers > _maxCharges)
            {
                _currentChargers -= 1;
                CurrentChargeChanged?.Invoke(_currentChargers);
            }
        }
    }

    private void ReductionCooldownForCharge(int index, float reductionTime)
    {
        var tempTime = reductionTime;
        if (tempTime > _remainingCooldownTimeChargers[index])
            return;

        if (_currentChargeCooldownJob[index] != null)
            StopCoroutine(_currentChargeCooldownJob[index]);

        _currentChargeCooldownJob[index] = StartCoroutine(RechargeOneChargeCoroutine(index, tempTime));
    }

    public virtual bool TryUseCharge()
    {
        if (_isUseCharges == false)
            return true;

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
                        _currentChargeCooldownJob[i] = StartCoroutine(RechargeOneChargeCoroutine(i, ChargeCooldown));

                        ChargeStartCooldown?.Invoke(ChargeCooldown);
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
            ChargeStartCooldown?.Invoke(ChargeCooldown);
            float time = 0;
            while (time < ChargeCooldown)
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
    #endregion CHARGES
    //Charges

    //Cooldowns
    #region Cooldown
    protected float _baseCooldownTime;
    private float _remainingCooldownTime;

    public float CooldownTime { get => Buff.Cooldown.GetBuffedValue(_cooldownTime); protected set => _cooldownTime = value; }
    public float RemainingCooldownTime { get => _remainingCooldownTime; set => _remainingCooldownTime = value; }
    public bool IsCooldowned { get => _remainingCooldownTime <= 0; }

    #region Cooldown Events
    public event Action CooldownEnded;
    public event Action<float> CooldownStarted;
    public event Action<float> MassageNotCooldowned;
    #endregion events

    #region Methods
    protected void RaiseCooldownStarted(float cooldownTime) => CooldownStarted?.Invoke(cooldownTime);

    protected void RaiseCooldownEnded() => CooldownEnded?.Invoke();

    public void IncreaseSetCooldown(float time)
    {
        if (time < _remainingCooldownTime)
            return;

        if (_cooldownJob != null)
            StopCoroutine(_cooldownJob);

        _cooldownJob = StartCoroutine(CooldownCoroutine(time));
    }

    public void IncreaseSetCooldownPassive(float time)
    {
        if (_cooldownJob != null) StopCoroutine(_cooldownJob);
        _cooldownJob = StartCoroutine(CooldownCoroutine(time));
    }

    public void ResetCooldown()
    {
        if (_cooldownJob != null)
        {
            StopCoroutine(_cooldownJob);
            _cooldownJob = null;
        }

        _remainingCooldownTime = 0;

        CooldownEnded?.Invoke();
    }

    public void DecreaseSetCooldown(float time)
    {
        var timeToSet = _remainingCooldownTime - time > 0 ? _remainingCooldownTime - time : 0;

        if (_cooldownJob != null)
            StopCoroutine(_cooldownJob);

        _cooldownJob = StartCoroutine(CooldownCoroutine(timeToSet));
    }
    public void ReductionSetCooldown(float time)
    {
        if (time > _remainingCooldownTime)
            return;

        if (_cooldownJob != null)
            StopCoroutine(_cooldownJob);

        _cooldownJob = StartCoroutine(CooldownCoroutine(time));
    }

    private IEnumerator CooldownCoroutine(float cooldownTime)
    {
        CooldownStarted?.Invoke(cooldownTime);
        _remainingCooldownTime = cooldownTime;

        while (_remainingCooldownTime > 0)
        {
            _remainingCooldownTime -= Time.deltaTime;
            yield return null;
        }
        CooldownEnded?.Invoke();
        _cooldownJob = null;
    }

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
        //трогать ивенты?
    }

    [Command]
    public void CmdCooldownModify(double delta)
    {
        if (!Cooldown.IsActive)
            return;

        _cooldownEndTime += delta;
        if (_cooldownEndTime >= NetworkTime.time)
        {
            Cooldown.ForceEnd();
        }
    }

    [Command]
    public void CmdCooldownEnd()
    {
        _cooldownEndTime = NetworkTime.time;
    }
    #endregion
    #endregion
    //Cooldowns

    //Targeting
    #region Targeting
    protected IHealable _tempForHealing;
    private Queue<TargetInfo> _targetInfoQueue = new();
    public Queue<TargetInfo> TargetInfoQueue { get => _targetInfoQueue; }

    public abstract void LoadTargetData(TargetInfo targetInfo);

    private bool IsValidTarget(IDamageable target) //оставить тут, сделать virutal?
    {
        if (target == null) return false;
        if (target is MonoBehaviour monoBehaviour) return monoBehaviour != null;

        return true;
    }

    private void SaveTargetData(TargetInfo targetInfo)
    {
        _targetInfoQueue.Enqueue(targetInfo);
    }

    private void LoadTargetDataForCheckCast() //Targeting.CheckDataForCast
    {
        if (_isCasting == false && _targetInfoQueue.TryPeek(out TargetInfo temp))
            LoadTargetData(temp);
    }
    #endregion Targeting
    //Targetning

    //Channeling
    #region Channeling

    #region Properties
    public float CastStreamDuration => Channeling.CastDuration; // Ctrl+R
    public float ManaCostRate { get => _manaCostRate; }
    public List<SkillEnergyCost> ManaCostPerTick { get => Channeling.Costs; }
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
            time += _manaCostRate;

            foreach (var skillCost in _manaCostPerTick)
            {
                var currentResourceValue = _hero.Resources[skillCost.resourceType].CurrentValue;

                if (currentResourceValue < Buff.ManaCost.GetBuffedValue(skillCost.resourceCost))
                {
                    TryCancel(true);
                }
                else
                {
                    var resource = _hero.Resources[skillCost.resourceType];
                    resource.CmdUse(Buff.ManaCost.GetBuffedValue(skillCost.resourceCost));
                }
            }
            yield return new WaitForSeconds(_manaCostRate);
        }
        _castStreamCoroutine = null;
        CastStreamEnded?.Invoke();
    }

    #endregion
    #endregion
    //Channeling

    //test counter
    protected float _currentCounter;

    private Coroutine _actionWrapperForPreparingCoroutine;
    private Coroutine _actionWrapperForCastCoroutine;
    private bool _isPreparing = false;
    private bool _isCasting = false;
    private TypeClick _click;
    private bool _isAutoMode;

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
    public bool IsSkillActive
    {
        get => _isSkillActive;
        set => _isSkillActive = value;
    }

    public bool GetMouseButton { get => _click != TypeClick.None; }
    public bool IsSubjectToGlobalCooldownTime { get => _isSubjectToGlobalCooldownTime; }
    public Character Hero { get => _hero; }
    public StatsBuff Buff => _statsBuff;
    public SkillAttributes Attributes => _skillAttributes; //TODO: Прикрепить SyncDictionary, чтобы аттрибуты синхронились по сети
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
    public bool IsHaveResources { get => IsHaveResourceOnSkill && IsCooldowned && IsHaveCharge; }
    public List<SkillEnergyCost> SkillEnergyCosts { get => _skillEnergyCosts; }
    public List<SkillEnergyCost> AdditionalSkillEnergyCosts { get => _additionalSkillEnergyCosts; }
    public float CastDeley { get => Buff.CastSpeed.GetBuffedValue(_castDeley); set => _castDeley = value; }
    public bool IsCasting { get => _isCasting; protected set => _isCasting = value; }
    public float MaxCounter { get => maxCounter; set => maxCounter = value; }
    public float CurrentCounter { get => _currentCounter; set => _currentCounter = value; }
    public virtual float Damage { get => _damageValue; set => _damageValue = value; }
    public float AutoAttackDelay { get => _autoAttackDelay; }
    public ChargeCDUI LinkedChargeCDUI { get; set; }
    public bool Disactive
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
    #endregion Properties

    #region AllEvents
    public event Action<bool> OnSkillStateChanged;
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
    public event Action BoostEnabled;
    public event Action BoostDisabled;
    public event Action<GameObject, Skill> OnDamageApplied;
    public event Action<GameObject, Skill> OnHealApplied;
    #endregion

    public int AnimTriggerCastPublic => AnimTriggerCast;
    /// <summary>
    /// There may be a description that will be shown in the AbillityNameBox.
    /// </summary>
    public virtual string AdditionalDescription { get; }
    protected abstract int AnimTriggerCastDelay { get; }
    protected abstract int AnimTriggerCast { get; }

    protected void SkillAfterCastJob() => AfterCast?.Invoke();
    protected void CastEndedJob() => CastEnded?.Invoke();
    protected void CurrentCharge(int charges) => CurrentChargeChanged?.Invoke(charges);

    protected virtual bool IsCanCast //важно потрогать
    {
        get
        {
            _targetInfoQueue.TryPeek(out TargetInfo temp);

            if (temp == null)
                return true;

            switch (Info.SkillType)
            {
                case SkillType.Target:

                    if (temp.GetTargets().Count > 0)
                    {
                        foreach (var target in temp.GetTargets())
                            if (Vector3.Distance(target.Position, transform.position) > AreaInfo.Radius)
                                return false;

                        return true;
                    }
                    else
                    {
                        return false;
                    }

                case SkillType.Projectile:

                    if (temp.GetTargets().Count > 0)
                    {
                        foreach (var target in temp.GetTargets())
                            if (Vector3.Distance(target.Position, transform.position) > AreaInfo.Radius)
                                return false;

                        return true;
                    }
                    else
                    {
                        return true;
                    }

                case SkillType.Zone:

                    if (temp.Points.Count > 0)
                    {
                        foreach (var point in temp.Points)
                            if (Vector3.Distance(point, transform.position) > AreaInfo.Radius)
                                return false;

                        return true;
                    }
                    else if (temp.GetTargets().Count > 0)
                    {
                        foreach (var target in temp.GetTargets())
                            if (Vector3.Distance(target.Position, transform.position) > AreaInfo.Radius)
                                return false;

                        return true;
                    }
                    else
                    {
                        return true;
                    }

                case SkillType.NonTarget:

                    return true;
                //обработать бесцельный с границей и кликом

                default:

                    return true;
            }
        }
    }

    protected abstract IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback);
    protected abstract IEnumerator CastJob();
    protected abstract void ClearData();

    protected virtual void SkillEnableBoostLogic() { }

    protected virtual void SkillDisableBoostLogic() { }

    public void Init(SkillRenderer render, Character hero)
    {
        _hero = hero;
        _skillRender = render;
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
        //CastBar, Sound, Animation
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

    //Возможно стоит сделать синглтон для Server-Side таймеров?
    //И сервер будет сообщать когда кд пошел, когда прошел
    //Мб лучше перевести на event, компонент в Init/OnEnable будет подписываться на него
    private void Update()
    {
        Cooldown?.Tick();
        //Charges.Tick(Time.deltaTime);
    }
    
    public void ClearQueueTarget() => _targetInfoQueue.Clear();

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

    #region Cast Related
    public bool TryPreparing()
    {
        if (_isPreparing == false)
        {
            foreach (var skillCost in _skillEnergyCosts)
            {
                //var currentResourceValue = _hero.Resources.Where(r => r.Type == skillCost.resourceType);
                var resource = _hero.Resources[skillCost.resourceType];
                resource.PhantomValueShow(skillCost.resourceCost);
            }
            _actionWrapperForPreparingCoroutine = StartCoroutine(ActionWrapperForPreparingJob());
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool TryCast()
    {

        if (_isCasting || _isPreparing)
            return false;

        LoadTargetDataForCheckCast();
        if (IsHaveResources && IsCanCast && _isCasting == false && Targeting.NoObstacles() && Hero.IsDead == false)
        {
            _isCasting = true;
            TryPayCost(IsPayCostStartCooldown);

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
                TryPayCost(IsPayCostStartCooldown);

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
            //var currentResourceValue = _hero.Resources.Where(r => r.Type == skillCost.resourceType);
            var resource = _hero.Resources[skillCost.resourceType];
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

                CastEnded?.Invoke();
            }

            CancelCoroutine(_castDeleyCoroutine);
            CancelCoroutine(_castStreamCoroutine);

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

            _hero.Animator.SetTrigger(HashAnimPlayer.AnimCancled);
            _hero.NetworkAnimator.SetTrigger(HashAnimPlayer.AnimCancled);
            OnSkillCanceled?.Invoke();

            return true;
        }
        else
        {
            Hero.Abilities.NotifySkillIsPreparing(this, false);
            return false;
        }
    }
    #endregion

    #region Resource Related
    public void CheckResources()
    {
        foreach (var skillCost in _skillEnergyCosts)
        {
            var currentResourceValue = _hero.Resources[skillCost.resourceType].CurrentValue;

            if (currentResourceValue < Buff.ManaCost.GetBuffedValue(skillCost.resourceCost))
            {
                float shortage = Buff.ManaCost.GetBuffedValue(skillCost.resourceCost) - currentResourceValue;

                switch (skillCost.resourceType)
                {
                    case ResourceType.Health:
                        MassageHaventMana?.Invoke(shortage);
                        break;
                    case ResourceType.Mana:
                        MassageHaventMana?.Invoke(shortage);
                        break;
                    case ResourceType.Energy:
                        MassageHaventMana?.Invoke(shortage);
                        break;
                    case ResourceType.Rune:
                        MassageHaventMana?.Invoke(shortage);
                        break;
                    default:
                        break;
                }
            }
        }

        if (IsCooldowned == false)
            MassageNotCooldowned?.Invoke(_remainingCooldownTime);

        if (IsHaveCharge == false)
            MassageHaventCharge?.Invoke();
    }

    private bool CheckResourcesOnSkill()
    {
        return _skillEnergyCosts.All(skillCost =>
            _hero.Resources[skillCost.resourceType].CurrentValue >= Buff.ManaCost.GetBuffedValue(skillCost.resourceCost));
    }

    protected virtual bool TryPayCost(List<SkillEnergyCost> skillEnergyCosts, bool startCooldown = true)
    {
        if (IsHaveResourceOnSkill)
        {
            foreach (var skillCost in skillEnergyCosts)
            {
                var resource = _hero.Resources[skillCost.resourceType];
                resource.CmdUse(Buff.ManaCost.GetBuffedValue(skillCost.resourceCost));

            }

            if (startCooldown)
            {
                Cooldown.Start();
                IncreaseSetCooldown(CooldownTime);
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

    #region Animation Related
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

    protected virtual void PlayCastAnim(bool value)
    {
        _isPlayCastAnim = value;
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

    protected Coroutine StartCastDeleyCoroutine()
    {
        _castDeleyCoroutine = StartCoroutine(CastDeleyJob(CastDeley));
        return _castDeleyCoroutine;
    }

    protected Coroutine StartCastDeleyCoroutine(float time)
    {
        _castDeleyCoroutine = StartCoroutine(CastDeleyJob(time));
        return _castDeleyCoroutine;
    }

    protected void CancelCoroutine(Coroutine coroutine)
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
        }
    }
    public void AddingDescriptionSet(bool value, string text)
    {
        AbilityInfoHero.AddingDescriptionSet(value, text);
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

    private IEnumerator CastDeleyJob(float delayTime)
    {
        CastDeleyStarted?.Invoke(delayTime);

        Hero.Animator.SetFloat(HashAnimPlayer.CastSpeed, Buff.CastSpeed.Multiplier);
        _hero.Animator.SetTrigger(AnimTriggerCastDelay);
        _hero.NetworkAnimator.SetTrigger(AnimTriggerCastDelay);

        float time = 0;

        while (time < delayTime)
        {
            if (Targeting.NoObstacles() == false)
            {
                TryCancel(true);
            }
            time += Time.deltaTime;
            yield return null;
        }
        _castDeleyCoroutine = null;
        CastDeleyEnded?.Invoke();
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

    private IEnumerator ActionWrapperForCastingJob()
    {
        Hero.Abilities.NotifySkillPrepared(this);
        CastStarted?.Invoke();
        _isCasting = true;

        bool noCast = Hero.Abilities.TryConsumeNoCast();

        if (!noCast && CastDeley > 0)
            yield return StartCastDeleyCoroutine();

        if (!noCast && AnimTriggerCast != 0)
        {
            _isPlayCastAnim = true;
            //_isWaitingForCastCoroutine = true;

            float finalCastSpeed = Buff.CastSpeed.Multiplier * ExtraAnimationSpeedMultiplier;
            Hero.Animator.SetFloat(HashAnimPlayer.CastSpeed, finalCastSpeed);
            _hero.Animator.SetTrigger(AnimTriggerCast);
            _hero.NetworkAnimator.SetTrigger(AnimTriggerCast);

            if (_forceFailCastEarly)
            {
                _forceFailCastEarly = false;

                _isCasting = false;
                _isPlayCastAnim = false;

                _hero.Animator.SetTrigger(HashAnimPlayer.AnimCancled);
                _hero.NetworkAnimator.SetTrigger(HashAnimPlayer.AnimCancled);
                _hero.Move.StopLookAt();
                Hero.Move.SetCanMove(true);

                ClearData();
                CastEnded?.Invoke();
                OnSkillCanceled?.Invoke();
                Canceled?.Invoke();
                _actionWrapperForCastCoroutine = null;
                Hero.UIComponent.Miss();
                yield return null;
            }

            while (_isPlayCastAnim)
            {
                //*
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
            _hero.Animator.SetTrigger(HashAnimPlayer.AnimCancled);
            _hero.NetworkAnimator.SetTrigger(HashAnimPlayer.AnimCancled);

            _castCoroutine = StartCoroutine(CastJob());
            if (_castDuration > 0) _castStreamCoroutine = StartCoroutine(CastStreamJob());
            yield return _castCoroutine;
        }

        _hero.Animator.SetTrigger(HashAnimPlayer.AnimCancled);
        _hero.NetworkAnimator.SetTrigger(HashAnimPlayer.AnimCancled);

        CastSuccess?.Invoke();
        CastEnded?.Invoke();
        _isCasting = false;

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
        _remainingCooldownTime = 0;

        if (_cooldownJob != null)
        {
            StopCoroutine(_cooldownJob);
            _cooldownJob = null;
        }
        CooldownEnded?.Invoke();

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
            _hero.DamageTracker.AddDamage(damage, target, isServerRequest: isServer);
            _hero.DamageGet(damage, target);
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
}
