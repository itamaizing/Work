using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#region Enums
#region Inspector Enums
[Flags]
public enum TargetLayer
{
    None = 0,
    Unit = 1 << 0,
    Ground = 1 << 1,
    Tree = 1 << 2,
}

[Flags]
public enum TargetFaction
{
    None = 0,
    Self = 1 << 0,
    Ally = 1 << 1,
    Enemy = 1 << 2,

    All = Self | Ally | Enemy,
}

[Flags]
public enum UnitType
{
    None = 0,
    Hero = 1 << 0,
    Building = 1 << 1,
    Creep = 1 << 2,
    Minion = 1 << 3, //ControlledCreep
}

public enum OutOfRangeClick // => Если за радиусом - всегда кастуем. Если за радиусом, но на цель - кастуем, когда цель будет в радиусе
{
    Queue,
    Cast,
}
#endregion

#region LogicEnums
public enum TargetType
{
    None,
    Point,
    Object,
}
public enum TargetingType
{
    /// <summary>
    /// Target - скилл ГАРАНТИРОВАННО применяется к цели. Можно скастовать ТОЛЬКО когда цель в радиусе.
    /// Вспышка света - снаряд, но он ТОЛЬКО таргетный и следует за целью => тип Target
    /// </summary>
    Target,
    /// <summary>
    /// Projectile - Летит по прямой в точку (будь-то клик или положение цели). Может промахнутся. Может попасть не в ту цель
    /// Стрела эльфийки - снаряд. Да, его можно применить к цели, но это не гарантирует попадание
    /// </summary>
    Projectile,
    Zone,
    NonTarget,
    NonTargetWithClick,
}
#endregion
#endregion Enums

[Serializable]
#if UNITY_EDITOR
public class TargetingComponent : BaseSkillComponent, ISerializationCallbackReceiver
{
    #region Editor-Only
    /// <summary>
    /// Для удобства редактирования, чтобы
    /// А) Слои для клика сами ставились на стандартные при смене типа способности
    /// Б) Не затирались значения, если дергать тип туда-сюда
    /// По идее не живет между запусками Юнити, да и ладно
    /// </summary>
    [SerializeField, HideInInspector] private Dictionary<TargetingType, TargetLayer> editorBackupValue = new();
    [SerializeField, HideInInspector] private TargetingType oldType = new();

    public void OnBeforeSerialize()
    {
        if (_targetingType != oldType)
        {
            if (!editorBackupValue.TryAdd(oldType, _clickLayer))
                editorBackupValue[oldType] = _clickLayer;

            if (editorBackupValue.TryGetValue(_targetingType, out var oldLayer))
                _clickLayer = oldLayer;
            else
                switch (_targetingType)
                {
                    case TargetingType.Target:
                        _clickLayer = TargetLayer.Unit;
                        break;

                    case TargetingType.Projectile:
                        _clickLayer = (TargetLayer.Unit | TargetLayer.Ground);
                        break;

                    case TargetingType.Zone:
                        _clickLayer = TargetLayer.Ground;
                        break;

                    case TargetingType.NonTarget:
                    case TargetingType.NonTargetWithClick:
                        _clickLayer = TargetLayer.None;
                        break;
                }
            oldType = _targetingType;
        }
    }

    public void OnAfterDeserialize() { }
    #endregion
#else
    public class TargetingComponent : BaseSkillComponent
{
#endif
    #region InspectorFields
    /// <summary>
    /// ТОЛЬКО на кого можем нажать. Unit - применяется к цели, Ground - прменяется на землю
    /// </summary>
    [SerializeField] protected TargetingType _targetingType;
    [SerializeField] protected TargetLayer _clickLayer;
    [SerializeField] protected TargetFaction _clickFaction;
    [SerializeField] protected UnitType _clickUnit;
    [SerializeField, Mirror.ReadOnly] protected LayerMask _clickMask;
    [SerializeField] protected LayerMask _interactionMask;  //Если не указали вручную - формируется по _clickFaction
    [SerializeField] protected OutOfRangeClick _outOfRangeBehaviour;
    [SerializeField] protected bool _needLineOfSight;
    #endregion

    #region Runtime Variables
    protected const float _defaultSearchRadius = 0.3f;
    protected LayerMask _obstacles;

    protected TargetData _target;
    protected TargetData _tempTarget;
    protected TargetData _forDamage;

    public event Action<Vector3> OnClick;
    #endregion

    #region Properties
    public LayerMask InteractionMask
    {
        get => _interactionMask;
        set => _interactionMask = value;
    }
    public LayerMask ClickMask
    {
        get => _clickMask;
        set => _clickMask = value;
    }
    public TargetingType SkillType { get => _targetingType; }
    public TargetFaction Faction
    {
        get => _clickFaction;
        set
        {
            _clickFaction = value;
            SetUpPhysicLayers();
        }
    }
    public UnitType Units { get => _clickUnit; }
    public OutOfRangeClick OutRange { get => _outOfRangeBehaviour; }

    public LayerMask AdditionalObstacle;

    public TargetData Target => _target;
    public TargetData Temporary
    {
        get => _tempTarget;
    }
    public TargetData ForDamage
    {
        get => _forDamage;
        set => _forDamage = value;
    }
    public bool NeedLineOfSight { get => _needLineOfSight; }
    #endregion

    #region Methods
    public override void Init(Skill skill)
    {
        base.Init(skill);
        SetUpPhysicLayers(_interactionMask.value==0);
    }

    #region Get-Set
    #region TempTarget
    public TargetData GetTempTarget(bool canTargetDead = false)
    {
        if (_tempTarget == null)
            return null;

        if (!_tempTarget.Targetable.IsTargetable && !canTargetDead)
            return null;
        return _tempTarget;
    }
    public void SetTempTarget(ITargetable character)
    {
        if (character == null)
            return;
        _tempTarget = new TargetData((character as MonoBehaviour)?.gameObject);
    }

    public void ClearTempTarget()
    {
        _tempTarget = null;
    }
    #endregion TempTarget

    #region Target
    public TargetData GetTarget(bool canTargetDead = false)
    {
        if (_target == null)
            return null;
        if (_target.Type == TargetType.Point)
            return _target;

        if (!(_target.Targetable?.IsTargetable ?? false) && !canTargetDead)
            return null;
        return _target;
    }

    public void SetTarget(ITargetable character)
    {
        if (character == null)
            return;
        _target = new TargetData((character as MonoBehaviour)?.gameObject);
    }

    public void SetTarget(TargetData target) // ??
    {
        if (target == null)
            return;
        _target = target;
    }

    public void ClearTarget()
    {
        _target = null;
    }
    #endregion Target
    #endregion Get-Set

    public TargetData QueueInfoToTargetData(TargetInfo targetInfo)
    {
        if (_clickLayer == TargetLayer.Unit)
        {
            if (targetInfo.GetTargets().Count == 0)
                return null;
            return new TargetData(targetInfo.GetTargets()[0].Transform.gameObject);
        }
        if (_clickLayer == TargetLayer.Ground)
        {
            if (targetInfo.Points.Count == 0)
                return null;
            return new TargetData(targetInfo.Points[0]);
        }

        if (targetInfo.GetTargets().Count > 0)
            return new TargetData(targetInfo.GetTargets()[0].Transform.gameObject);

        if (targetInfo.Points.Count > 0)
            return new TargetData(targetInfo.Points[0]);

        return null;
    }

    public bool CanCast(TargetData target, float? radius = null)
    {
        if (target == null && (_targetingType & (TargetingType.NonTargetWithClick | TargetingType.NonTarget)) == 0)
            return false;

        Vector3 point = new();
        switch (target.Type)
        {
            case TargetType.Point:
                point = target.Point;
                break;

            case TargetType.Object:
                point = target.Transform.position;
                break;
        }

        if (!radius.HasValue)
            radius = _skill.CastDistance;
        switch (_targetingType)
        {
            case TargetingType.Target:
                return (IsPointInRadius(radius.Value, point));

            case TargetingType.Projectile:
            case TargetingType.Zone:
                if (!IsPointInRadius(radius.Value, point))
                {
                    if (_outOfRangeBehaviour == OutOfRangeClick.Queue || target.Type == TargetType.Object)
                        return false;
                }
                return true;

            case TargetingType.NonTargetWithClick:
            case TargetingType.NonTarget:
                return true;

            default:
                Debug.LogError("Не проверяем такой тип скилла");
                throw new NotImplementedException();
        }
    }

    public TargetData GetTargetOrPoint(float searchRadius = _defaultSearchRadius, bool useLayerMask = true)
    {
        if (SkillType == TargetingType.NonTargetWithClick)
            return new TargetData(GetMousePoint(useLayerMask: false));

        var clickPoint = GetMousePoint(useLayerMask: useLayerMask);
        if (clickPoint == null || clickPoint == Vector3.zero)
            return null;

        if (_clickLayer.HasFlag(TargetLayer.Ground) && !_clickLayer.HasFlag(TargetLayer.Unit)) //Если ждем только точку - возвращаем точку
        {
            return new TargetData(clickPoint);
        }

        var targets = FindTargets(clickPoint, searchRadius, canTargetSelf: (_clickFaction.HasFlag(TargetFaction.Self)));
        if (targets == null || targets.Count <= 0) //Если не нашли цель
        {
            if (_clickLayer.HasFlag(TargetLayer.Ground)) // и можно по земле
            {
                return new TargetData(clickPoint);
            }
            return null;
        }
        else if (_clickLayer.HasFlag(TargetLayer.Unit)) //Если нашли цель - проверяем команду
        {
            foreach (var target in targets)
            {
                if ((_clickMask & (1 << target.Object.layer)) != 0)
                    return target;
            }
        }
        return null;
    }

    /// <summary>
    /// Находит и устанавливает tempTarget, берет текущую точку курсора и значение радиуса из скилла
    /// </summary>
    public TargetData FindTempTarget(bool? canTargetSelf = null, bool canTargetDead = false)
    {
        return FindTempTarget(GetMousePoint(), _defaultSearchRadius, canTargetSelf.HasValue ? canTargetSelf.Value : _clickFaction.HasFlag(TargetFaction.Self), canTargetDead);
    }

    /// <summary>
    /// Находит и устанавилвает _tempTarget
    /// </summary>
    public TargetData FindTempTarget(Vector3 position, float radius, bool? canTargetSelf = null, bool canTargetDead = false)
    {
        var targets = FindTargets(position, radius, canTargetSelf.HasValue ? canTargetSelf.Value : _clickFaction.HasFlag(TargetFaction.Self), canTargetDead);
        if (targets == null || targets.Count <= 0)
        {
            ClearTempTarget(); //Возможно отсюда нужно вынести ниже, но вроде нет.
            return null;
        }
        _tempTarget = targets[0];
        return _tempTarget;
    }

    /// <summary>
    /// Предконечный метод поиска целей. Позволяет отфильтровать мертвых
    /// </summary>
    public List<TargetData> FindTargets(Vector3 position, float radius, bool? canTargetSelf = null, bool canTargetDead = false)
    {
        List<TargetData> targets = GetClosestTargets(position, radius, canTargetSelf.HasValue ? canTargetSelf.Value : _clickFaction.HasFlag(TargetFaction.Self));
        if (targets == null || targets.Count <= 0)
        {
            return null;
        }

        if (canTargetDead)
        {
            return targets;
        }
        else
        {
            return targets.Where(t => t.Targetable != null && t.Targetable.IsTargetable).ToList() ?? null;
        }
    }

    /// <summary>
    /// Конечный метод поиска цели
    /// </summary>
    public List<TargetData> GetClosestTargets(Vector3 position, float radius, bool? canTargetSelf = null, bool useLayerMask = true)
    {
        var targets = _character.TargetSeeker.GetCloserTargets(position, radius,
            canTargetSelf.HasValue ? canTargetSelf.Value : _clickFaction.HasFlag(TargetFaction.Self), useLayerMask ? _clickMask : null);
        if (targets == null || targets.Count <= 0)
        {
            //return new();
            return null;
        }
        List<TargetData> targetsData = new();
        foreach (var target in targets)
        {
            targetsData.Add(new TargetData((target as MonoBehaviour)?.gameObject));
        }
        return targetsData;
    }

    #region Helpers
    public bool IsTargetInRadius(float radius, Transform target)
    {
        if (target == null)
            return false;

        float distance = Vector3.Distance(target.position, _character.transform.position);
        return distance <= radius;
    }

    public bool IsPointInRadius(float radius, Vector3 point)
    {
        float distance = Vector3.Distance(point, _character.transform.position);
        return distance <= radius;
    }

    public Vector3 GetMousePoint(bool useLayerMask = false)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        LayerMask mask = useLayerMask ? _clickMask : (LayerMask.GetMask("Default", "Ground", "Obstecls"));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, mask))
        {
            //Я не очень понимаю зачем это нужно было раньше, вероятно можно смело удалять
            if (_skill.Info.AutoAttack == AutoAttack.autoAttack)
            {
                if (UnityEngine.InputSystem.Keyboard.current.leftCtrlKey.isPressed) //?
                {
                    if (hit.collider.TryGetComponent<IDamageable>(out _))
                    {
                        //Уже неактуально?
                        //_skill.IsAutoMode = true;
                        //_skill.AutoModeChanged?.Invoke(true);
                    }
                }
            }

            return hit.point;
        }
        return Vector3.zero;
    }

    public bool IsMouseInRadius(float radius)
    {
        float distance = Vector3.Distance(GetMousePoint(), _character.transform.position);

        return distance <= radius;
    }

    /// <summary>
    /// Обрезает точку до макс. значения радиуса способности
    /// </summary>
    public Vector3 ClampToRadius(Vector3 center, Vector3 point, float radius)
    {
        Vector3 direction = point - center;
        return center + Vector3.ClampMagnitude(direction, radius);
    }

    public bool NoObstacles(Vector3 target, Vector3 point, LayerMask obstacle)
    {
        if (target == Vector3.zero)
            return true;

        var vector = (target - point);
        var dir = vector.normalized;
        float distance = vector.magnitude;

        RaycastHit[] rayHit = Physics.RaycastAll(point, dir, distance, obstacle);

        if (rayHit.Length > 0)
            return false;
        else
            return true;
    }

    public bool NoObstacles(Vector3 target, LayerMask obstacle)
    {
        return NoObstacles(target, _character.transform.position, obstacle);
    }

    public bool NoObstacles()
    {
        if (_target != null)
            return NoObstacles(_target.Position, _character.transform.position, _obstacles);

        return true;
    }
    #endregion Helpers

    private void SetUpPhysicLayers(bool updateInteraction=true)
    {
        LayerMask clickMask = 0;
        LayerMask interactionMask = 0;
        if ((_clickLayer & TargetLayer.Ground) != 0 && (_targetingType & (TargetingType.Zone | TargetingType.Projectile)) != 0)
        {
            clickMask |= LayerMask.GetMask("Ground");
        }

        if ((_clickLayer & TargetLayer.Unit) != 0)
        {
            if ((_clickFaction & TargetFaction.Enemy) != 0)
            {
                clickMask |= LayerMask.GetMask("Enemy");
            }
            if ((_clickFaction & (TargetFaction.Self | TargetFaction.Ally)) != 0)
            {
                clickMask |= LayerMask.GetMask("Allies");
            }
        }
        _clickMask = clickMask;

        _obstacles = LayerMask.GetMask("Obstecls");

        if (!updateInteraction)
            return;

        if ((_clickFaction & (TargetFaction.Self | TargetFaction.Ally)) != 0)
            interactionMask |= LayerMask.GetMask("Allies");
        if ((_clickFaction & (TargetFaction.Self | TargetFaction.Enemy)) != 0)
            interactionMask |= LayerMask.GetMask("Enemy");
        _interactionMask = interactionMask;
    }
    #endregion Methods
}

//По-хорошему TargetInfo -> TargetQueue, где внутри List<TargetData>?
public class TargetData
{
    public TargetType Type;
    public GameObject Object;
    public Vector3 Point;

    public TargetData(Vector3 point)
    {
        Type = TargetType.Point;
        Point = point;
        Object = null;
    }

    public TargetData(GameObject gameObject)
    {
        Type = TargetType.Object;
        Point = Vector3.positiveInfinity;
        Object = gameObject;
    }

    public Vector3 Position
    {
        get
        {
            if (Type == TargetType.Point)
                return Point;
            if (Type == TargetType.Object)
                return Transform.position;
            return Vector3.positiveInfinity;
        }
    }

    public Transform Transform => Object == null ? null : Object.transform;
    public ITargetable Targetable => Object == null ? null : Object.GetComponent<ITargetable>();
    public IHealable Healable => Object == null ? null : Object.GetComponent<IHealable>();
    public IDamageable Damageable => Object == null ? null : Object.GetComponent<IDamageable>();
    public Character Character => Object == null ? null : Object.GetComponent<Character>();
}