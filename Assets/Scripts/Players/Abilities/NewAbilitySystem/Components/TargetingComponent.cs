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
}

[Flags]
public enum UnitType
{
    None = 0,
    Hero = 1 << 0,
    Building = 1 << 1,
    Creep = 1 << 2,
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
#endregion
#endregion Enums
[Serializable]
#if UNITY_EDITOR
public class TargetingComponent : BaseSkillComponent, ISerializationCallbackReceiver
#else
public class TargetingComponent : BaseSkillComponent
#endif
{
    #region InspectorFields
    /// <summary>
    /// ТОЛЬКО на кого можем нажать. Unit - применяется к цели, Ground - прменяется на землю
    /// </summary>
    [SerializeField] protected SkillType type;
    [SerializeField] protected TargetLayer _clickLayer; //возможно стоит разделить клик от физ. взаимодействия
    [SerializeField] protected TargetFaction _faction;
    [SerializeField] protected UnitType _unitType;
    [SerializeField] protected OutOfRangeClick _outOfRangeBehaviour;
    #endregion

#if UNITY_EDITOR
    /// <summary>
    /// Для удобства редактирования, чтобы
    /// А) Слои для клика сами ставились на стандартные при смене типа способности
    /// Б) Не затирались значения, если дергать тип туда-сюда
    /// По идее не живет между запусками Юнити, да и ладно
    /// </summary>
    private Dictionary<SkillType, TargetLayer> editorBackupValue = new ();
    private SkillType oldType = new();

    public void OnBeforeSerialize()
    {
        if (type != oldType)
        {
            if (!editorBackupValue.TryAdd(oldType, _clickLayer))
                editorBackupValue[oldType] = _clickLayer;

            if (editorBackupValue.TryGetValue(type, out var oldLayer))
                _clickLayer = oldLayer;
            else
                switch (type)
                {
                    case SkillType.Target:
                        _clickLayer = TargetLayer.Unit;
                        break;

                    case SkillType.Projectile:
                        _clickLayer = (TargetLayer.Unit | TargetLayer.Ground);
                        break;

                    case SkillType.Zone:
                        _clickLayer = TargetLayer.Ground;
                        break;

                    case SkillType.NonTarget:
                    case SkillType.NonTargetWithClick:
                        _clickLayer = TargetLayer.None;
                        break;
                }
        }
        oldType = type;
    }

    public void OnAfterDeserialize() { }
#endif

    #region Runtime Variables
    protected LayerMask _targetLayer;
    protected LayerMask _obstacles;

    protected TargetData _target;
    protected TargetData _tempTarget;
    protected TargetData _forDamage;

    public event Action<Vector3> OnClick;
    #endregion

    #region Properties
    public LayerMask Layer {
        get => _targetLayer;
        set => _targetLayer = value;
    }

    public TargetData Target => _target;
    public TargetData Temporary { 
        get => _tempTarget;
    }
    public TargetData ForDamage
    {
        get => _forDamage;
        set => _forDamage = value;
    }
    #endregion

    #region Methods
    public override void Init(Skill skill)
    {
        base.Init(skill);
        SetUpPhysicLayers();
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

    public bool CanCast(TargetInfo target=null)
    {
        if (target == null)
            return true;    //True? Может стоит проверять тип скилла?


        //таргетный - никогда не клэмпим
        //нонтаргет - сразу true
        //если допустимы и цель и точка:
        //Для ЦЕЛЕЙ: никогда не округляем
        //Для ТОЧЕК: в зависимости от _outOfRangeBehaviour

        //Остается понять как по TargetInfo догадаться на кого мы нажали
        //и в каких случаях у нас может быть несколько целей/точек именно внутри TargetInfo

        // var range = 0; //if target/zone => radius | projectile => castLength? || Max(castLength, Radius)?
        var range = _skill.AreaInfo.Radius;
        switch (_skill.Info.SkillType)
        {
            case SkillType.Target:
                if (target.GetTargets().Count > 0)
                {
                    foreach (var t in target.GetTargets())
                        if (!IsTargetInRadius(_skill.AreaInfo.Radius, t.Transform))
                            return false;

                    return true;
                }
                return false;

            case SkillType.Zone:
            case SkillType.Projectile:
                List<Vector3> pointsToCheck = new();
                if (_clickLayer == TargetLayer.Unit)
                    foreach (var unit in target.GetTargets())
                        pointsToCheck.Add(unit.Transform.position);
                if (_clickLayer == TargetLayer.Ground)
                    foreach (var point in target.Points)
                        pointsToCheck.Add(point);

                if (pointsToCheck.Count == 0)
                    return false;           //Возвращал True, но это странно, у нас же не указана цель

                for (int i = 0; i < pointsToCheck.Count; i++)
                {
                    if (!IsPointInRadius(_skill.AreaInfo.Radius, pointsToCheck[i]))
                    {
                        if (_outOfRangeBehaviour == OutOfRangeClick.Queue)
                            return false;
                        //else
                        //  pointsToCheck[i] = ClampToRadius(_character.Position, pointsToCheck[i], _skill.AreaInfo.Radius); | => LoadData
                        //Не много ли чести для простого bool метода проверки дальности?
                        //+ это НЕ СРАБОТАЕТ, надо менять точки напрямую в target
                        //При этом нельзя менять position юнита, т.к. этол его переместит
                        // См. коммент выше. Вроде таргет инфо все-таки всегда содержит точку.
                        // Если есть перс - считаем, что клик был по герою, иначе - по земле
                        // Но вообще, я бы переписал TargetInfo => на свою TargetData

                        // Мб лучше клэмпить уже в CastJob
                    }
                }
                return true;

            case SkillType.NonTargetWithClick:
            case SkillType.NonTarget:
                return true;

            default:
                Debug.LogError("Не проверяем такой тип скилла");
                throw new NotImplementedException();
        }
        //return true;
    }

    // Мне не очень нравится, что в одном месте мы проверяем SkillType, а в другом ClickLayer
    // Как будто бы стоит писать все в одном формате. Тогда более главенствующий/информативный - скорее ClickLayer
    public bool CanCast(TargetData target)
    {
        if (target == null)
            return true;    //True? Может стоит проверять тип скилла?
        
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

        switch (type)
        {
            case SkillType.Target:
                if (!IsPointInRadius(_skill.AreaInfo.Radius, point))
                    return false;
                return true;

            case SkillType.Projectile:
            case SkillType.Zone:
                if (!IsPointInRadius(_skill.AreaInfo.Radius, point))
                {
                    if (_outOfRangeBehaviour == OutOfRangeClick.Queue || target.Type == TargetType.Object)
                        return false;
                }
                return true;

            case SkillType.NonTargetWithClick:
            case SkillType.NonTarget:
                return true;

            default:
                Debug.LogError("Не проверяем такой тип скилла");
                throw new NotImplementedException();
        }
        //return true;
    }

    public TargetData GetTargetOrPoint(float searchRadius = 0.3f)
    {
        var clickPoint = GetMousePoint(useLayerMask: true);
        if ((_clickLayer & TargetLayer.Unit) == 0 && (_clickLayer & TargetLayer.Ground) != 0) //Если ждем только точку - возвращаем точку
        {
            return new TargetData(clickPoint);
        }

        var targets = FindTargets(clickPoint, searchRadius, canTargetSelf: (_faction & TargetFaction.Self) != 0);
        if (targets == null || targets.Count <= 0) //Если не нашли цель
        {
            if ((_clickLayer & TargetLayer.Ground) != 0) // Если нельзя по земле
            {
                return new TargetData(clickPoint);
            }
            return null;
        }
        else if ((_clickLayer & TargetLayer.Unit) != 0) //Если нашли цель - проверяем команду
        {
            foreach (var target in targets) 
            {
                if ((_targetLayer & (1 << target.Object.layer)) != 0)
                    return target;
            }
        }
        return null;
    }
    
    public TargetData FindTempTarget(bool canTargetSelf = false, bool canTargetDead = false)
    {
        return FindTempTarget(GetMousePoint(), _skill.AreaInfo.Radius, canTargetSelf, canTargetDead);
    }

    public TargetData FindTempTarget(Vector3 position, float radius, bool canTargetSelf = false, bool canTargetDead = false)
    {
        var targets = FindTargets(position, radius, canTargetSelf, canTargetDead);
        if (targets == null || targets.Count <= 0)
        {
            ClearTempTarget(); //Возможно отсюда нужно вынести ниже, но вроде нет.
            return null;
        }
        _tempTarget = targets[0];
        return _tempTarget;
    }

    public List<TargetData> FindTargets(Vector3 position, float radius, bool canTargetSelf=false, bool canTargetDead=false)
    {
        List<TargetData> targets = GetClosestTargets(position, radius, canTargetSelf);
        if (targets == null || targets.Count <= 0)
        {
            //ClearTempTarget(); Почему это вообще тут есть, это же внешние методы
            return new();
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

    public List<TargetData> GetClosestTargets(Vector3 position, float radius, bool canTargetSelf = false)
    {
        var targets = _character.TargetSeeker.GetCloserTargetsCharacter(position, radius, canTargetSelf);
        if (targets == null || targets.Count <= 0)
        {
            //ClearTempTarget();
            return new();
        }
        List<TargetData> targetsData = new();
        foreach (var target in targets)
        {
            targetsData.Add(new TargetData(target.gameObject));
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

    public Vector3 GetMousePoint(bool useLayerMask = false) //добавить в Raycast() layerMask
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        LayerMask mask = useLayerMask ? _targetLayer : (LayerMask.GetMask("Default", "Ground", "Obstecls"));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, mask))
        {
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
        if (_tempTarget != null)
            return NoObstacles(_tempTarget.Character.transform.position, _character.transform.position, _obstacles);

        return true;
    }
    #endregion Helpers

    private void SetUpPhysicLayers()
    {
        LayerMask layerMask = 0;
        if ((_clickLayer & TargetLayer.Ground) != 0)
        {
            layerMask |= LayerMask.GetMask("Ground");
        }

        if ((_clickLayer & TargetLayer.Unit) != 0)
        {
            if ((_faction & TargetFaction.Enemy) != 0)
            {
                layerMask |= LayerMask.GetMask("Enemy");
            }
            if ((_faction & (TargetFaction.Self | TargetFaction.Ally)) != 0)
            {
                layerMask |= LayerMask.GetMask("Allies");
            }
        }
        _targetLayer = layerMask;

        _obstacles = LayerMask.GetMask("Obstecls");
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

    public Vector3 Poisition
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