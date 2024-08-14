using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public enum Schools
{
    Light,
    Dark,
    Fire,
    Water,
    Air,
    Earth,
    Physical,
    None
}

public enum AbilityForm
{
    Spell,
    Magic,
    Physical
}

public abstract class Skill : MonoBehaviour
{
    [Header("AbilitieInfo")]
    [SerializeField] private AbilityInfo _abilityInfo;
    [Header("Main Settings")]
    [SerializeField] protected float _manaCost;
    [SerializeField] protected float _cooldownTime;
    [SerializeField] protected float _castDeley;
    [SerializeField] protected Schools _abilitySchool;
    [SerializeField] protected AbilityForm _abilityForm;
    [SerializeField] protected LayerMask _targetsLayers;
    [Header("Streaming settings")]
    [SerializeField] protected float _castDuration;
    [SerializeField] protected float _manaCostRate;
    [SerializeField] protected float _manaCostPerTick;
    [Header("Charge settings")]
    [SerializeField] protected bool _isUseCharges;
    [SerializeField] protected bool _chargesHaveSeparateCooldown;
    [SerializeField] protected int _maxCharges;
    [SerializeField] protected float _chargeCooldown;
    [Header("Area settings")]
    [SerializeField] protected float _radius;
    [SerializeField] protected float _area;
    [SerializeField] protected float _castLength;
    [SerializeField] protected float _castWidth;
    [Header("Render settings")]
    [SerializeField] protected bool _isAutoRadiusRender = true;
    [SerializeField] protected bool _isAutoAreaRender = true;
    [SerializeField] protected bool _isAutoLineRender = true;

    protected SkillRenderer _skillRender;
    protected HeroComponent _hero;
    protected bool _isCanCancle = true;
    protected int _currentChargers;
    protected Coroutine _prepareCoroutine;
    protected Coroutine _castCoroutine;
    protected Coroutine _cooldownJob;
    protected Coroutine _rechargeJob;
    protected Coroutine _castDeleyCoroutine;
    protected Coroutine _castStreamCoroutine;

    private float _remaining—ooldownTime;
    private StatsBuff _statsBuff = new StatsBuff();
    private Coroutine _actionWrapperForPreparingCoroutine;
    private Coroutine _actionWrapperForCastCoroutine;
    private bool _isPreparing = false;
    private bool _isCasting = false;

    public HeroComponent Hero { get => _hero; }
    public StatsBuff Buff => _statsBuff;
    public string Name => _abilityInfo.Name;
    public string Description => _abilityInfo.Description;
    public Sprite Icon => _abilityInfo.Icon;
    public bool IsCooldowned { get => _remaining—ooldownTime < 0; }
    public int Chargers => _currentChargers;
    public bool IsHaveCharge => (_currentChargers > 0);
    public float ChargeCooldown => _chargeCooldown;
    public bool IsPreparing => _isPreparing;
    public bool IsHaveManaOnSkill { get => ManaCost <= _hero.Stamina.Value; }
    public bool IsHaveResurces { get => IsHaveManaOnSkill && IsCooldowned && IsHaveCharge; }
    public float ManaCost { get => Buff.ManaCost.GetBuffedValue(_manaCost); }
    public float CooldownTime { get => Buff.Cooldown.GetBuffedValue(_cooldownTime); }
    public float CastDeley { get => Buff.CastSpeed.GetBuffedValue(_castDeley); }
    public bool IsCasting { get => _isCasting; }
    public float CastStreamDuration { get => _castDuration; }
    public float Radius { get => Buff.Radius.GetBuffedValue(_radius); protected set => _radius = value; }
    public float Area { get => Buff.Area.GetBuffedValue(_area); protected set => _area = value; }
    public float CastLength { get => Buff.Area.GetBuffedValue(_castLength); protected set => _castLength = value; }
    public float CastWidth { get => Buff.Area.GetBuffedValue(_castWidth); protected set => _castWidth = value; }
    public LayerMask TargetsLayers => _targetsLayers;
    public Schools School => _abilitySchool;
    public AbilityForm AbilityForm => _abilityForm;

    public event Action<int> CurrentChargeChange;
    public event Action<float> CooldownStarted;
    public event Action CooldownEnded;
    public event Action PreparingStarted;
    public event Action PreparingSuccess;
    public event Action<float> CastDeleyStarted;
    public event Action CastDeleyEnded;
    public event Action<float> CastStreamStarted;
    public event Action CastStreamEnded;
    public event Action CastStarted;
    public event Action CastEnded;
    public event Action Canceled;
    public event Action<float> MassageHaventMana;
    public event Action MassageHaventCharge;
    public event Action<float> MassageNotCooldowned;

    protected abstract bool IsCanCast { get; }

    protected abstract IEnumerator PrepareJob();
    protected abstract IEnumerator CastJob();
    protected abstract void ClearData();

    public void Init(SkillRenderer render, HeroComponent hero)
    {
        _hero = hero;
        _skillRender = render;
    }

    protected virtual void Awake()
    {
        if (_isUseCharges)
            _currentChargers = _maxCharges;
        else
            _currentChargers = 1;
    }

    public bool TryPreparing()
    {
        if(_isPreparing == false && _isCasting == false)
        {
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
        if (TryPayCost() && IsCanCast && _isCasting == false)
        {
            _actionWrapperForCastCoroutine = StartCoroutine(ActionWrapperForCastingJob());
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool TryCancel(bool foceCancel = false)
    {
        if (foceCancel || _isCanCancle)
        {
            Canceled?.Invoke();
            ClearData();
            CancelCoroutine(_castCoroutine);
            CancelCoroutine(_castDeleyCoroutine);
            CancelCoroutine(_castStreamCoroutine);

            if (_actionWrapperForPreparingCoroutine != null)
            {
                StopCoroutine(_actionWrapperForPreparingCoroutine);
                CancelCoroutine(_prepareCoroutine);
                _actionWrapperForPreparingCoroutine = null;
                _isPreparing = false;
                StopAutoDraw();
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    public void SetCooldown(float time)
    {
        if (time < _remaining—ooldownTime)
            return;

        if (_cooldownJob != null)
            StopCoroutine(_cooldownJob);

        _cooldownJob = StartCoroutine(CooldownCoroutine(time));
    }

    public void CheckResources()
    {
        if (IsHaveManaOnSkill == false)
            MassageHaventMana?.Invoke(ManaCost - _hero.Stamina.Value);

        if (IsCooldowned == false)
            MassageNotCooldowned?.Invoke(_remaining—ooldownTime);

        if (IsHaveCharge == false)
            MassageHaventCharge?.Invoke();
    }

    protected virtual void StartAutoDraw()
    {
        if(_isAutoRadiusRender)
            _skillRender.DrawRadius(Radius);

        if (_isAutoAreaRender)
            _skillRender.DrawArea(Area, TargetsLayers);

        if (_isAutoLineRender)
            _skillRender.DrawLine(CastLength, CastWidth, TargetsLayers);
    } 

    protected virtual void StopAutoDraw()
    {
        _skillRender.StopDrawRadius();
        _skillRender.StopDrawArea();
        _skillRender.StopDrawLine();
    }

    protected virtual bool TryPayCost(float mana)
    {
        if (IsHaveResurces)
        {
            _hero.Stamina.CmdUse(mana);
            SetCooldown(CooldownTime);
            TryUseCharge();
            return true;
        }
        else
        {
            return false;
        }
    }

    protected virtual bool TryPayCost()
    {
        return TryPayCost(_manaCost);
    }

    protected Character GetRaycastTarget(bool isCanTargetHimself = false)
    {
        Character target = null;
        RaycastHit2D[] rayHit = Physics2D.RaycastAll(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero, 99, TargetsLayers);

        foreach (var item in rayHit)
        {
            if (rayHit.Length > 0 && item.transform.TryGetComponent<Character>(out Character enemy))
            {
                target = enemy;

                if (isCanTargetHimself == false && target.transform == _hero.Health.transform)
                {
                    target = null;
                }
            }
        }
        return target;
    } 

    protected List<Character> GetCloserTargets(Vector3 position, float radius, bool isCanTargetHimself = false)
    {
        List<Character>  targets = new List<Character>();
        Collider2D[] collider = Physics2D.OverlapCircleAll(position, radius, TargetsLayers);

        foreach (var item in collider)
        {
            if (collider.Length > 0 && item.transform.TryGetComponent<Character>(out Character enemy))
            {
                if (isCanTargetHimself == false && targets[targets.Count - 1].transform == _hero.Health.transform)
                {
                    continue;
                }
                targets.Add(enemy);
            }
        }
        targets = targets.OrderBy(character => Vector3.Distance(character.transform.position, gameObject.transform.position)).ToList();
        return targets;
    }

    protected bool IsTargetInRadius(float radius, Transform target)
    {
        if (target == null)
            return false;

        float distance = Vector3.Distance(target.position, transform.position);
        return distance <= radius;
    }

    protected bool IsPointInRadius(float radius, Vector3 point)
    {
        float distance = Vector3.Distance(point, transform.position);
        return distance <= radius;
    }

    protected Coroutine StartCastDeleyCoroutine()
    {
        _castDeleyCoroutine = StartCoroutine(CastDeleyJob());
        return _castDeleyCoroutine;
    }

    protected void CancelCoroutine(Coroutine coroutine)
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
        }
    }

    protected bool IsMouseInRadius(float radius)
    {
        float distance = Vector3.Distance(
            new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, transform.position.z),
            transform.position
            );

        return distance <= radius;
    }

    protected Vector2 GetMousePoint()
    {
        return Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    private bool TryUseCharge()
    {
        if (_isUseCharges == false)
            return true;

        if (_currentChargers > 0)
        {
            _currentChargers--;
            CurrentChargeChange?.Invoke(_currentChargers);

            if (_rechargeJob == null || _chargesHaveSeparateCooldown)
                _rechargeJob = StartCoroutine(RechargeCoroutine());
            return true;
        }
        else
        {
            return false;
        }
    }

    private IEnumerator RechargeCoroutine()
    {
        while (_currentChargers < _maxCharges)
        {
            float time = 0;
            while (time < ChargeCooldown)
            {
                time += Time.deltaTime;
                yield return null;
            }
            _currentChargers++;
            CurrentChargeChange?.Invoke(_currentChargers);
        }
        _rechargeJob = null;
    }

    private IEnumerator CooldownCoroutine(float cooldownTime)
    {
        CooldownStarted?.Invoke(cooldownTime);
        _remaining—ooldownTime = cooldownTime;

        while (_remaining—ooldownTime > 0)
        {
            _remaining—ooldownTime -= Time.deltaTime;
            yield return null;
        }
        CooldownEnded?.Invoke();
        _cooldownJob = null;
    }

    private IEnumerator CastDeleyJob()
    {
        CastDeleyStarted?.Invoke(CastDeley);
        float time = 0;

        while (time < CastDeley)
        {
            time += Time.deltaTime;
            yield return null;
        }
        _castDeleyCoroutine = null;
        CastDeleyEnded?.Invoke();
    }

    private IEnumerator CastStreamJob()
    {
        CastStreamStarted?.Invoke(CastStreamDuration);
        float time = 0;

        while (time < CastStreamDuration)
        {
            time += _manaCostRate;
            _hero.Stamina.Use(_manaCostPerTick);

            yield return new WaitForSeconds(_manaCostRate);
        }
        _castStreamCoroutine = null;
        CastStreamEnded?.Invoke();
    }

    private IEnumerator ActionWrapperForPreparingJob()
    {
        PreparingStarted?.Invoke();
        _isPreparing = true;
        StartAutoDraw();

        yield return _prepareCoroutine = StartCoroutine(PrepareJob());

        PreparingSuccess?.Invoke();
        _isPreparing = false;
        StopAutoDraw();

        _prepareCoroutine = null;
    }

    private IEnumerator ActionWrapperForCastingJob()
    {
        CastStarted?.Invoke();
        _isCasting = true;

        if (CastDeley > 0)
            yield return StartCastDeleyCoroutine();

        if (_castDuration > 0)
            StartCoroutine(CastStreamJob());

        yield return _castCoroutine = StartCoroutine(CastJob());

        CastEnded?.Invoke();
        _isCasting = false;

        ClearData();

        _castCoroutine = null;
    }
}
