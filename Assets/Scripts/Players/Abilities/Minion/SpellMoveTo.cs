using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SpellMoveTo : Skill
{
    [SerializeField] private float _moveDurationPerUnit = 0.2f;
    [SerializeField] private float _damageDelay = 0.5f;
    [SerializeField] private float _attackDistance = 3f;
    [SerializeField] private float _damage = 5f;
    [SerializeField] private Animator _animator;

    private Queue<Vector3> _movementQueue = new();
    private bool _isCasting = false;
    private Coroutine _attackCoroutine;
    private Character _currentEnemyTarget;
    private float _lastAttackTime;

    public Action<GameObject> DoMove;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => !_isCasting;

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.Points != null && targetInfo.Points.Count > 0)
        {
            foreach (var point in targetInfo.Points)
            {
                _movementQueue.Enqueue(point);
            }
        }
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (!GetMouseButton)
        {
            yield return null;
        }

        Vector3 clickedPoint = GetMousePoint();

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Points.Add(clickedPoint);
        callbackDataSaved(targetInfo);
    }
    protected override IEnumerator CastJob()
    {
        _isCasting = true;

        while (_movementQueue.Count > 0)
        {
            Vector3 point = _movementQueue.Dequeue();

            if (_attackCoroutine != null)
            {
                StopCoroutine(_attackCoroutine);
                _attackCoroutine = null;
            }

            float distance = Vector3.Distance(transform.position, point);
            float duration = distance * _moveDurationPerUnit;

            yield return MoveToPoint(point, duration);

            _attackCoroutine = StartCoroutine(AttackNearbyEnemiesJob());
        }

        _isCasting = false;
    }

    private IEnumerator MoveToPoint(Vector3 targetPoint, float duration)
    {
        Hero.Move.CanMove = false;

        Vector3 direction = (targetPoint - transform.position).normalized;
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = lookRotation;
        }

        float moved = 0f;
        Vector3 prevPos = transform.position;

        Tween moveTween = transform.DOMove(targetPoint, duration)
            .SetEase(Ease.Linear)
            .OnUpdate(() =>
            {
                float delta = Vector3.Distance(transform.position, prevPos);
                moved += delta;
                prevPos = transform.position;

                if (moved >= 1f)
                {
                    DoMove?.Invoke(gameObject);
                    moved = 0f;
                }
            });

        yield return moveTween.WaitForCompletion();

        Hero.Move.CanMove = true;
    }
    private IEnumerator AttackNearbyEnemiesJob()
    {
        while (true)
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
                float distanceToTarget = Vector3.Distance(transform.position, nearest.transform.position);


                if (distanceToTarget > _attackDistance && distanceToTarget <= Radius)
                {
                    float duration = distanceToTarget * _moveDurationPerUnit;
                    yield return MoveToPoint(nearest.transform.position, duration);
                }


                while (nearest != null && !nearest.IsDead && Vector3.Distance(transform.position, nearest.transform.position) <= Radius)
                {
                    float dist = Vector3.Distance(transform.position, nearest.transform.position);


                    if (dist <= _attackDistance && Time.time - _lastAttackTime > _damageDelay)
                    {
                        _currentEnemyTarget = nearest;
                        _animator.SetTrigger("AutoAttackScared");
                        _lastAttackTime = Time.time;
                        yield return new WaitForSeconds(_damageDelay);
                    }
                    else
                    {
                        yield return null;
                    }
                }
            }
            else
            {
                yield break;
            }


            yield return null;
        }
    }
    private void DealDamage()
    {
        if (_currentEnemyTarget == null) return;

        Damage damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(_damage),
            Type = DamageType,
            PhysicAttackType = AttackRangeType
        };

        CmdApplyDamage(damage, _currentEnemyTarget.gameObject);
    }

    public void OnAutoAttackAnimationHit()
    {
        if (_currentEnemyTarget == null) return;
        DealDamage();
    }

    public void OnAutoAttackAnimationEnd()
    {
        _currentEnemyTarget = null;
    }

    protected override void ClearData()
    {
        _movementQueue.Clear();
        _currentEnemyTarget = null;
        _isCasting = false;
    }
}
