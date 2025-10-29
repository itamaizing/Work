using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SpellMoveTo : Skill
{

    [SerializeField] private NavMeshAgent _agent;
    [SerializeField] private LayerMask _alliesLayer;
    [SerializeField] private float _damageDelay = 0.5f;
    [SerializeField] private float _attackDistance = 3f;
    [SerializeField] private Animator _animator;

    private Vector3 _targetPoint = Vector3.positiveInfinity;
    private Vector3 _runtimeTargetPoint = Vector3.positiveInfinity;
    protected IDamageable _target = null;
    private Character _runtimeTarget = null;
    private Character _currentTargetForAnim;
    private Coroutine _attackCoroutine;
    private Coroutine _followAllyCoroutine;
    private bool _isChainedAttack = false;
    private bool _isAttacking = false;
    private float _lastAttackTime;

    private Queue<Vector3> _movementQueue = new();

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => true;

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.Points != null && targetInfo.Points.Count > 0) _targetPoint = targetInfo.Points[0];
    }

    public void OnAutoAttackAnimationHit()
    {
        if (_currentTargetForAnim == null || _currentTargetForAnim.IsDead) return;

        _target = _currentTargetForAnim;
        DealDamage();
    }
    public void OnAutoAttackAnimationEnd()
    {
        _isAttacking = false;
        _currentTargetForAnim = null;
    }
    protected virtual void DealDamage()
    {
        if (_target == null) return;

        Damage damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(_damageValue),
            Type = DamageType,
            PhysicAttackType = AttackRangeType,
        };

        CmdApplyDamage(damage, _target.gameObject);
    }

    protected override IEnumerator CastJob()
    {
        _isChainedAttack = false;
        bool isAllyTarget = _runtimeTarget != null && ((_alliesLayer.value & (1 << _runtimeTarget.gameObject.layer)) != 0);

        if (isAllyTarget)
        {
            if (_followAllyCoroutine != null) StopCoroutine(_followAllyCoroutine);
            _followAllyCoroutine = StartCoroutine(FollowAllyCoroutine());
        }

        else yield return StartCoroutine(MoveToPointCoroutine());
    }

    protected override void ClearData()
    {
        _agent.SetDestination(transform.position);
        _targetPoint = Vector3.positiveInfinity;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        while (float.IsPositiveInfinity(_targetPoint.x))
        {
            if (GetMouseButton)
            {
                _target = GetRaycastTarget();

                if (_target == null)
                {
                    _targetPoint = GetMousePoint();
                }
                else
                {
                    if (_target is Character characterTarget) _runtimeTarget = characterTarget;
                    _targetPoint = _target.transform.position;
                }

                _runtimeTargetPoint = _targetPoint;
                _movementQueue.Enqueue(_runtimeTargetPoint);

            }
            yield return null;
        }

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Points.AddRange(_movementQueue);
        targetDataSavedCallback(targetInfo);
    }
    private void AttackTargetWithAnimation(Character target)
    {
        if (_isAttacking || target == null || target.IsDead) return;

        _isAttacking = true;
        _currentTargetForAnim = target;

        _animator.SetTrigger("AutoAttackScared");
    }

    private IEnumerator MoveToPointCoroutine()
    {
        while (Vector3.Distance(transform.position, _runtimeTargetPoint) > _agent.stoppingDistance + 0.1f)
        {
             _agent.SetDestination(_runtimeTargetPoint);

            yield return new WaitForSeconds(0.1f);
        }

        if (_attackCoroutine != null) StopCoroutine(_attackCoroutine);
        _attackCoroutine = StartCoroutine(AttackNearbyEnemiesJob());
    }

    private IEnumerator FollowAllyCoroutine()
    {
        while (_runtimeTarget != null && !_runtimeTarget.IsDead)
        {
            _agent.SetDestination(_runtimeTarget.transform.position);

            yield return new WaitForSeconds(0.1f);
        }

        _agent.SetDestination(transform.position);
    }

    private IEnumerator AttackNearbyEnemiesJob()
    {
        _isChainedAttack = true;
        bool foundEnemy = false;

        while (_isChainedAttack)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, Radius, LayerMask.GetMask("Enemy"));

            Character nearest = null;
            float minDist = float.MaxValue;

            foreach (var hit in hits)
            {
                Character enemy = hit.GetComponent<Character>();
                if (enemy != null && !enemy.IsDead)
                {
                    float dist = Vector3.Distance(transform.position, enemy.transform.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearest = enemy;
                    }
                }
            }

            if (nearest != null)
            {
                _agent.SetDestination(nearest.transform.position);

                while (nearest != null && !nearest.IsDead && Vector3.Distance(transform.position, nearest.transform.position) > Radius)
                {
                    yield return null;
                }

                if (nearest != null && !nearest.IsDead && Time.time - _lastAttackTime > _damageDelay)
                {
                    float dist = Vector3.Distance(transform.position, nearest.transform.position);

                    if (dist <= _attackDistance)
                    {
                        AttackTargetWithAnimation(nearest);
                        _lastAttackTime = Time.time;
                    }
                }

                yield return new WaitForSeconds(_damageDelay);
            }
            else
            {
                _isChainedAttack = false;
                break;
            }
        }

        if (!foundEnemy && !_runtimeTargetPoint.Equals(Vector3.positiveInfinity))
        {
            _agent.SetDestination(_runtimeTargetPoint);
        }
    }
}
