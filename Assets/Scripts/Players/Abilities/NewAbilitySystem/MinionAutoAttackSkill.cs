using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class MinionAutoAttackSkill : Skill, IPassiveSkill
{
    [Header("Minion Auto Attack Settings")]
    [SerializeField] private NavMeshAgent _agent;
    [SerializeField] private float _minDamage = 1f;
    [SerializeField] private float _maxDamage = 3f;
    [SerializeField] private float _attackInterval = 1.2f;
    [SerializeField] private float _searchInterval = 0.25f;
    [SerializeField] private float _targetSearchRadiusMultiplier = 1.1f;

    private static readonly int AttackTrigger = Animator.StringToHash("Attack");

    public enum MinionAttackOrder
    {
        None,
        Move,
        AttackTarget,
        AttackPoint
    }

    private MinionAttackOrder _currentOrder = MinionAttackOrder.None;
    private Character _target;
    private Vector3 _orderPoint;
    private MinionComponent _minion;
    private bool _isPlayingCastAnim;
    private Character _pendingDamageTarget;

    private Coroutine _aiLoopCoroutine;

    public Character Target => _target;
    public MinionAttackOrder CurrentOrder => _currentOrder;
    
    public virtual float TargetSearchRadius => AreaInfo.Radius * _targetSearchRadiusMultiplier;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    private void Awake()
    {
        _minion = Hero as MinionComponent;
    }

    private void OnEnable()
    {
        if (_aiLoopCoroutine != null) StopCoroutine(_aiLoopCoroutine);
        _aiLoopCoroutine = StartCoroutine(MinionAILoop());
    }

    private void OnDisable()
    {
        if (_aiLoopCoroutine != null)
        {
            StopCoroutine(_aiLoopCoroutine);
            _aiLoopCoroutine = null;
        }
        StopAgent();
    }

    public void OrderMove(Vector3 point)
    {
        AbortCastIfAny();
        _target = null;
        _orderPoint = point;
        _currentOrder = MinionAttackOrder.Move;
        if (_minion != null) _minion.LastOrder = MinionComponent.MinionOrder.Move;
        MoveToTarget(point);
    }

    public void OrderAttackTarget(Character target)
    {
        AbortCastIfAny();
        _target = target;
        _currentOrder = MinionAttackOrder.AttackTarget;
        if (_minion != null) _minion.LastOrder = MinionComponent.MinionOrder.AutoAttack;
    }

    public void OrderAttackPoint(Vector3 point)
    {
        AbortCastIfAny();
        _target = null;
        _orderPoint = point;
        _currentOrder = MinionAttackOrder.AttackPoint;
        if (_minion != null) _minion.LastOrder = MinionComponent.MinionOrder.AutoAttack;
        MoveToTarget(point);
    }

    private void AbortCastIfAny()
    {
        if (IsCasting) TryCancel(true);
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        TargetData targetData = null;
        while (targetData == null)
        {
            if (GetMouseButton)
            {
                targetData = Targeting.GetTargetOrPoint();

                if (targetData == null)
                {
                    Vector3 groundPoint = Targeting.GetMousePoint(useLayerMask: false);
                    if (groundPoint != Vector3.zero)
                        targetData = new TargetData(groundPoint);
                }
            }
            yield return null;
        }

        switch (targetData.Type)
        {
            case TargetType.Object:
                Character clickedCharacter = targetData.Character;
                if (clickedCharacter != null)
                {
                    bool isEnemy = _hero == null || _hero.RelationTo(clickedCharacter) == CharacterRelation.Enemy;
                    if (isEnemy)
                        OrderAttackTarget(clickedCharacter);
                }
                break;

            case TargetType.Point:
                OrderAttackPoint(targetData.Point);
                break;
        }
    }
    
    protected override IEnumerator CastJob()
    {
        Character currentTarget = Targeting.GetTarget()?.Character;
        if (currentTarget == null || currentTarget.IsDead)
            yield break;

        if (_hero != null)
        {
            if (_hero.Animator != null)
                _hero.Animator.SetTrigger(AttackTrigger);

            if (_hero.NetworkAnimator != null)
                _hero.NetworkAnimator.SetTrigger(AttackTrigger);
        }

        ApplyMinionDamage(currentTarget);

        float delay = _attackInterval;
        if (Buff != null && Buff.AttackSpeed != null)
        {
            delay = Buff.AttackSpeed.GetBuffedValue(_attackInterval);
        }

        yield return new WaitForSeconds(delay);
    }

    private IEnumerator MinionAILoop()
    {
        var searchWait = new WaitForSeconds(_searchInterval);

        while (true)
        {
            if (IsCasting)
            {
                yield return null;
                continue;
            }

            switch (_currentOrder)
            {
                case MinionAttackOrder.Move:
                    TickMoveOrder();
                    break;

                case MinionAttackOrder.AttackPoint:
                    TickAttackPointOrder();
                    break;

                case MinionAttackOrder.AttackTarget:
                    TickAttackTargetOrder();
                    break;

                case MinionAttackOrder.None:
                default:
                    break;
            }

            yield return searchWait;
        }
    }

    private void TickMoveOrder()
    {
        if (HasReachedPoint(_orderPoint))
        {
            StopAgent();
        }
        else
        {
            MoveToTarget(_orderPoint);
        }
    }

    private void TickAttackPointOrder()
    {
        Character enemy = FindNearestEnemy(TargetSearchRadius);
        if (enemy != null)
        {
            _target = enemy;
            _currentOrder = MinionAttackOrder.AttackTarget;
            return;
        }

        if (HasReachedPoint(_orderPoint))
        {
            StopAgent();
            _currentOrder = MinionAttackOrder.AttackTarget;
            _target = null;
        }
        else
        {
            MoveToTarget(_orderPoint);
        }
    }

    private void TickAttackTargetOrder()
    {
        if (_target == null || _target.IsDead)
        {
            StopAgent();
            _target = FindNearestEnemy(TargetSearchRadius);
            return;
        }

        float distance = Vector3.Distance(transform.position, _target.transform.position);
        bool inAttackRange = distance <= AreaInfo.Radius;
        bool hasLineOfSight = Targeting.NoObstacles(_target.transform.position, _obstacle);

        if (inAttackRange && hasLineOfSight)
        {
            StopAgent();

            TargetInfo targetInfo = new TargetInfo();
            targetInfo.AddTarget(_target);
            TryCast(targetInfo);
        }
        else
        {
            MoveToTarget(_target.transform.position);
        }
    }

    private void ApplyMinionDamage(Character target)
    {
        if (target == null || target.IsDead) return;

        Damage damage = new Damage
        {
            Value = UnityEngine.Random.Range(_minDamage, _maxDamage),
            Type = Info.DamageType,
            PhysicAttackType = Info.AttackRangeType,
            School = Info.School,
            Form = Info.AbilityForm,
        };

        if (Buff != null && Buff.Damage != null)
        {
            damage.Value = Buff.Damage.GetBuffedValue(damage.Value);
        }

        CmdApplyDamage(damage, target.gameObject);
    }

    private Character FindNearestEnemy(float radius)
    {
        if (Hero == null || Hero.TargetSeeker == null) return null;

        var candidates = Hero.TargetSeeker.GetCloserTargetsCharacter(transform.position, radius, false);
        if (candidates == null) return null;

        int enemyLayer = LayerMask.NameToLayer("Enemy");

        return candidates.FirstOrDefault(c =>
            c != null &&
            !c.IsDead &&
            (c.gameObject.layer == enemyLayer || c.NetworkSettings?.TeamIndex != Hero.NetworkSettings?.TeamIndex)
        );
    }

    private void MoveToTarget(Vector3 destination)
    {
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = false;
            _agent.SetDestination(destination);
        }
    }

    private void StopAgent()
    {
        if (_agent != null && _agent.isOnNavMesh && !_agent.isStopped)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }
    }

    private bool HasReachedPoint(Vector3 point)
    {
        if (_agent == null) return true;
        if (_agent.pathPending) return false;

        float stoppingDistance = Mathf.Max(_agent.stoppingDistance, 0.1f);
        return _agent.remainingDistance <= stoppingDistance &&
               (!_agent.hasPath || _agent.velocity.sqrMagnitude < 0.01f);
    }

    protected override void ClearData()
    {
        base.ClearData();
    }
}