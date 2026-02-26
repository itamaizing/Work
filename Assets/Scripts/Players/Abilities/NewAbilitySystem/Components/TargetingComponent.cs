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
public class TargetingComponent : BaseSkillComponent
{
    #region InspectorFields
    /// <summary>
    /// ТОЛЬКО на кого можем нажать. Unit - применяется к цели, Ground - прменяется на землю
    /// </summary>
    [SerializeField] protected TargetLayer _clickLayer; //возможно стоит разделить клик от физ. взаимодействия
    [SerializeField] protected TargetFaction _faction;
    [SerializeField] protected UnitType _unitType;
    [SerializeField] protected OutOfRangeClick _outOfRangeBehaviour;
    #endregion

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

        if (!_target.Targetable.IsTargetable && !canTargetDead)
            return null;
        return _target;
    }

    public void SetTarget(ITargetable character)
    {
        if (character == null)
            return;
        _target = new TargetData((character as MonoBehaviour)?.gameObject);
    }

    public void ClearTarget()
    {
        _target = null;
    }
    #endregion Target
    #endregion Get-Set
    
    public TargetData FindTempTarget(bool canTargetSelf = false, bool canTargetDead = false)
    {
        return FindTempTarget(GetMousePoint(), _skill.AreaInfo.Radius, canTargetSelf, canTargetDead);
    }
    
    public TargetData FindTempTarget(Vector3 position, float radius, bool canTargetSelf = false, bool canTargetDead = false)
    {
        var targets = FindTargets(position, radius, canTargetSelf, canTargetDead);
        if (targets == null || targets.Count <= 0)
            return null;
        _tempTarget = targets[0];
        return _tempTarget;
    }

    public List<TargetData> FindTargets(Vector3 position, float radius, bool canTargetSelf=false, bool canTargetDead=false)
    {
        List<TargetData> targets = GetClosestTargets(position, radius, canTargetSelf);
        if (targets == null || targets.Count <= 0)
        {
            ClearTempTarget();
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
            ClearTempTarget();
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

    public Transform Transform => Object == null ? null : Object.transform;
    public ITargetable Targetable => Object == null ? null : Object.GetComponent<ITargetable>();
    public IHealable Healable => Object == null ? null : Object.GetComponent<IHealable>();
    public IDamageable Damageable => Object == null ? null : Object.GetComponent<IDamageable>();
    public Character Character => Object == null ? null : Object.GetComponent<Character>();
}