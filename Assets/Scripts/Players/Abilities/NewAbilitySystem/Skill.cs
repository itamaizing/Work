using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class SkillEnergyCost
{
    public ResourceType resourceType;
    public float resourceCost;
}

public struct TargetToShot
{
    public Vector2 Position;
    public Character character;
}


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

public enum SkillType
{
    Target,
    Projectile,
    Zone
}

public abstract class Skill : NetworkBehaviour
{
    [Header("AbilitiesInfo")]
    [SerializeField] private AbilityInfo _abilityInfo;
    [Header("Main Settings")]
    [SerializeField] protected bool _isSubjectToGlobalCooldownTime = true;

    [SerializeField] protected List<SkillEnergyCost> _skillEnergyCosts;
    [SerializeField] protected float _cooldownTime;
    [SerializeField] protected float _castDeley;
    [SerializeField] protected float _damageValue;
    [SerializeField] private Schools _abilitySchool;
    [SerializeField] private AbilityForm _abilityForm;
    [SerializeField] private DamageType _damageType;
    [SerializeField] private AttackRangeType _attackRangeType;
    [SerializeField] private SkillType _skillType;
    [SerializeField] protected LayerMask _targetsLayers;
    [SerializeField] protected LayerMask _obstacle;
    [Header("Streaming settings")]
    [SerializeField] protected float _castDuration;
    [SerializeField] protected float _manaCostRate;
    [SerializeField] protected float _manaCostPerTick;
    [Header("Charge settings")]
    [SerializeField] private bool _isUseCharges;
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
    protected Character _hero;
    protected bool _isCanCancle = true;
    protected Coroutine _prepareCoroutine;
    protected Coroutine _castCoroutine;
    protected Coroutine _cooldownJob;
    protected Coroutine _rechargeJob;
    protected Coroutine _castDeleyCoroutine;
    protected Coroutine _castStreamCoroutine;
    protected Transform _tempTargetForDamage;
    protected Health _tempHPForDamage;
    protected Character _tempTarget;

    private int _currentChargers;
    private float _remainingCooldownTime;
    private StatsBuff _statsBuff = new StatsBuff();
    private Coroutine _actionWrapperForPreparingCoroutine;
    private Coroutine _actionWrapperForCastCoroutine;
    private bool _isPreparing = false;
    private bool _isCasting = false;
    private bool _isClick;
    private bool _isShiftClick;
    private bool _isCtrlClick;

    public bool GetMouseButton { get => _isClick || _isShiftClick || _isCtrlClick; }
    public bool IsSubjectToGlobalCooldownTime { get => _isSubjectToGlobalCooldownTime; }
    public Character Hero { get => _hero; }
    public StatsBuff Buff => _statsBuff;
    public string Name => _abilityInfo.Name;
    public string Description => _abilityInfo.Description;
    public Sprite Icon => _abilityInfo.Icon;
    public bool IsCooldowned { get => _remainingCooldownTime <= 0; }
    public virtual bool IsPayCostStartCooldown { get => true; }
    public int Chargers { get => _currentChargers; protected set { _currentChargers = value; CurrentChargeChanged?.Invoke(_currentChargers); } }
    public bool IsHaveCharge => (_currentChargers > 0);
    public float ChargeCooldown => _chargeCooldown;
    public bool IsPreparing => _isPreparing;
    public bool IsHaveResourceOnSkill { get => CheckResourcesOnSkill(); }
    public bool IsHaveResources { get => IsHaveResourceOnSkill && IsCooldowned && IsHaveCharge; }
    public float CooldownTime { get => Buff.Cooldown.GetBuffedValue(_cooldownTime); protected set => _cooldownTime = value; }
    public float CastDeley { get => Buff.CastSpeed.GetBuffedValue(_castDeley); protected set => _castDeley = value; }
    public bool IsCasting { get => _isCasting; }
    public float CastStreamDuration { get => _castDuration; }
    public float Radius { get => Buff.Radius.GetBuffedValue(_radius); protected set => _radius = value; }
    public float Area { get => Buff.Area.GetBuffedValue(_area); protected set => _area = value; }
    public float CastLength { get => Buff.Area.GetBuffedValue(_castLength); protected set => _castLength = value; }
    public float CastWidth { get => Buff.Area.GetBuffedValue(_castWidth); protected set => _castWidth = value; }
    public virtual float Damage  { get => Buff.Damage.GetBuffedValue(_damageValue); protected set => _damageValue = value; }
    public bool IsUseCharges { get => _isUseCharges; }
    public LayerMask TargetsLayers { get => _targetsLayers; protected set => _targetsLayers = value; }
    public Schools School { get => _abilitySchool; protected set => _abilitySchool = value; }
    public AbilityForm AbilityForm => _abilityForm;
    public DamageType DamageType => _damageType;
    public AttackRangeType AttackRangeType => _attackRangeType;
    public SkillType SkillType => _skillType;

    public event Action<int> CurrentChargeChanged;
    public event Action<float> CooldownStarted;
    public event Action CooldownEnded;
    public event Action PreparingStarted;
    public event Action PreparingSuccess;
    public event Action PreparingCanceled;
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

    public void Init(SkillRenderer render, Character hero)
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
        if (_isPreparing == false && _isCasting == false)
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
        if (IsHaveResources && IsCanCast && _isCasting == false && NoObstacles())
        {
            TryPayCost(IsPayCostStartCooldown);
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
                StopAutoDraw();

                PreparingCanceled?.Invoke();

				UnSubscribeClickEvents();
				OnClickCanceled();
            }

            _tempTarget = null;

            return true;
        }
        else
        {
            return false;
        }
    }

    public void IncreaseSetCooldown(float time)
    {
        if (time < _remainingCooldownTime)
            return;

        if (_cooldownJob != null)
            StopCoroutine(_cooldownJob);

        _cooldownJob = StartCoroutine(CooldownCoroutine(time));
    }

    public void ReductionSetCooldown(float time)
    {
        if (time > _remainingCooldownTime)
            return;

        if (_cooldownJob != null)
            StopCoroutine(_cooldownJob);

        _cooldownJob = StartCoroutine(CooldownCoroutine(time));
    }

    public void CheckResources()
    {
        foreach (var skillCost in _skillEnergyCosts)
        {
            var currentResourceValue = _hero.Resources.Where(r => r.Type == skillCost.resourceType).Sum(r => r.CurrentValue);

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
            _hero.Resources.Where(r => r.Type == skillCost.resourceType).Sum(r => r.CurrentValue) >= Buff.ManaCost.GetBuffedValue(skillCost.resourceCost));
    }

    public void AddMaxChargeCount()
    {
        _maxCharges += 1;
        _currentChargers += 1;
        CurrentChargeChanged?.Invoke(_currentChargers);
    }

    public void DeductMaxChargeCount()
    {
        if (_maxCharges - 1 > 0)
        {
            _maxCharges -= 1;

            if (_currentChargers > _maxCharges)
            {
                _currentChargers -= 1;
                CurrentChargeChanged?.Invoke(_currentChargers);
            }
        }
    }

    public void DrawDamageZone(Vector3 position)
    {
		Damage damage = new Damage
		{
			Value = Damage,
			Type = DamageType,
			Range = AttackRangeType,
		};
		_skillRender.CmdDrawDamageZone(position, Area, damage, _hero.gameObject);
    }

    public void StopDamageZone()
    {
        _skillRender.CmdStopDrawDamageZone();
    }

    protected virtual void StartAutoDraw()
    {
		Damage damage = new Damage
		{
			Value = Damage,
			Type = DamageType,
			Range = AttackRangeType,
		};

		if (_isAutoRadiusRender)
            _skillRender.DrawRadius(Radius);

        if (_isAutoAreaRender)
            _skillRender.DrawArea(Area, damage, TargetsLayers);

        if (_isAutoLineRender)
            _skillRender.DrawLine(CastLength, CastWidth, damage, TargetsLayers);
    }

    protected virtual void StopAutoDraw()
    {
        _skillRender.StopDrawRadius();
        _skillRender.StopDrawArea();
        _skillRender.StopDrawLine();
    }

    protected virtual bool TryPayCost(List<SkillEnergyCost> skillEnergyCosts, bool startCooldown = true)
    {
        if (IsHaveResourceOnSkill)
        {
            foreach (var skillCost in skillEnergyCosts)
            {
                var resource = _hero.Resources.First(r => r.Type == skillCost.resourceType);
                resource.CmdUse(Buff.ManaCost.GetBuffedValue(skillCost.resourceCost));
            }

            if (startCooldown)
                IncreaseSetCooldown(CooldownTime);

            TryUseCharge();
            return true;
        }
        else
        {
            return false;
        }
    }

    protected virtual bool TryPayCost(bool startCooldown = true)
    {
        return TryPayCost(_skillEnergyCosts, startCooldown);
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
        _tempTarget = target;
        return target;
    }

    protected List<Character> GetCloserTargets(Vector3 position, float radius, bool isCanTargetHimself = false)
    {
        List<Character> targets = new List<Character>();
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

    protected bool NoObstacles(Vector3 target, Vector3 point, LayerMask obstacle)
    {
        if (target == Vector3.zero)
            return true;

        var vector = (target - point);
        var dir = vector.normalized;
        float distance = vector.magnitude;

        RaycastHit2D[] rayHit = Physics2D.RaycastAll(point, dir, distance, obstacle);

        if (rayHit.Length > 0)
            return false;
        else
            return true;
    }

    protected bool NoObstacles(Vector3 target, LayerMask obstacle)
    {
        return NoObstacles(target, transform.position, obstacle);
    }

    protected bool NoObstacles()
    {
        if (_tempTarget != null)
            return NoObstacles(_tempTarget.transform.position, transform.position, _obstacle);

        return true;
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

    protected bool TryUseCharge()
    {
        if (_isUseCharges == false)
            return true;

        if (_currentChargers > 0)
        {
            _currentChargers--;
            CurrentChargeChanged?.Invoke(_currentChargers);

            if (_rechargeJob == null || _chargesHaveSeparateCooldown)
                _rechargeJob = StartCoroutine(RechargeCoroutine());
            return true;
        }
        else
        {
            return false;
        }
    }

    protected virtual IEnumerator RechargeCoroutine()
    {
        while (_currentChargers < _maxCharges)
        {
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
            if (_chargesHaveSeparateCooldown)
                break;
        }
        _rechargeJob = null;
    }

    protected TargetToShot Actions()
    {
		TargetToShot target = new TargetToShot();

		if (_isClick)
        {
            return LeftClick();
        }
        if(_isShiftClick)
        {
            return ShiftLeftClick();
        }
        if(_isCtrlClick)
        {
            return CtrlLeftClick();
		}

        return target;
    }    

    protected TargetToShot LeftClick()
    {
        TargetToShot target = new TargetToShot();
        switch (_skillType)
        {
            case SkillType.Target:
                target.character = GetCloserTargets(transform.position, 100)[0];
                break; 
            case SkillType.Projectile:
				target.character = GetCloserTargets(transform.position, 100)[0];
				break;
			case SkillType.Zone:
				target.Position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
				break;
            default:
				target.Position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
				break;
		}
        return target;
	}

    protected TargetToShot ShiftLeftClick()
    {
		TargetToShot target = new TargetToShot();
		/*switch (_skillType)
		{
			case SkillType.Target:
				//auto attack mode
				target.Position = GetCloserTargets(transform.position, 100)[0];
                break;
			case SkillType.Projectile:
				target.Position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
				break;
			case SkillType.Zone:
				target.Position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
				break;
			default:
				target.Position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
				break;
		}*/
        target.Position = transform.position;
        target.character = _hero;

		return target;
	}

	protected TargetToShot CtrlLeftClick()
	{
		TargetToShot target = new TargetToShot();
		switch (_skillType)
		{
			case SkillType.Target:
				target.character = GetCloserTargets(transform.position, 100)[0];
                break;
			case SkillType.Projectile:
				target.Position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
				break;
			case SkillType.Zone:
				target.Position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
				break;
			default:
				target.Position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
				break;
		}
		return target;
	}

    protected Vector2 GetClosestTarget()
    {
		Collider2D[] enemyDetected = Physics2D.OverlapCircleAll(transform.position, 100);
        Vector2 closest = Vector2.positiveInfinity;
        foreach (Collider2D collider in enemyDetected)
        {
            if (collider.gameObject != _hero.gameObject)
            
            if(collider.TryGetComponent<Character>(out var enemy))
            {
                if(Vector2.Distance(collider.transform.position, transform.position) < Vector2.Distance(closest, transform.position))
                {
                    closest = collider.transform.position;
                    Debug.Log(enemy);
                }
            }
        }
        if(Vector2.Distance(closest, transform.position) < 100)   return closest;
        else return Camera.main.ScreenToWorldPoint(Input.mousePosition);

	}


	private void OnClick()
    {
        _isClick = true;
    }

    private void OnClickCanceled()
    {
        _isClick = false;
    }

    private void OnShiftClick()
    {
        _isShiftClick = true;
    }

	private void OnShiftCancled()
	{
		_isShiftClick = false;
	}

    private void OnCtrlClick()
    {
        _isCtrlClick = true;
    }

    private void OnCtlCancled()
    {
        _isCtrlClick = false;
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

    private IEnumerator CastDeleyJob()
    {
        CastDeleyStarted?.Invoke(CastDeley);
        float time = 0;

        while (time < CastDeley)
        {
            if (NoObstacles() == false)
            {
                TryCancel(true);
            }
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
            if (_hero.Stamina.CurrentValue >= _manaCostPerTick)
            {
                _hero.Stamina.TryUse(_manaCostPerTick);
            }
            else
            {
                TryCancel(true);
            }
            yield return new WaitForSeconds(_manaCostRate);
        }
        _castStreamCoroutine = null;
        CastStreamEnded?.Invoke();
    }

    private IEnumerator ActionWrapperForPreparingJob()
    {
        PreparingStarted?.Invoke();
        _isPreparing = true;
        ClearData();
        StartAutoDraw();

        SubscribeClickEvents();

		yield return _prepareCoroutine = StartCoroutine(PrepareJob());

        UnSubscribeClickEvents();

		OnClickCanceled();

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

    [Command]
    protected void CmdApplyDamage(Damage damage, GameObject hp)
    {
        if (_tempTargetForDamage != hp.transform)
        {
            _tempTargetForDamage = hp.transform;
            _tempHPForDamage = hp.GetComponent<Health>();
        }
        _tempHPForDamage.TryTakeDamage(ref damage, this);
    }

    private void SubscribeClickEvents()
    {
		InputHandler.OnClick += OnClick;
        InputHandler.OnShiftLeftMouse += OnShiftClick;
        InputHandler.OnSwitchAutoMode += OnCtrlClick;

        //cancelled

		InputHandler.OnClickCanceled += OnClickCanceled;
        InputHandler.OnShiftLeftMouseCanceled += OnShiftCancled;
        InputHandler.OnSwitchAutoModeCanceled += OnCtlCancled;
        
	}

	private void UnSubscribeClickEvents()
	{
		InputHandler.OnClick -= OnClick;
		InputHandler.OnShiftLeftMouse -= OnShiftClick;
		InputHandler.OnSwitchAutoMode -= OnCtrlClick;

		//cancelled

		InputHandler.OnClickCanceled -= OnClickCanceled;
		InputHandler.OnShiftLeftMouseCanceled -= OnShiftCancled;
		InputHandler.OnSwitchAutoModeCanceled -= OnCtlCancled;

	}
}