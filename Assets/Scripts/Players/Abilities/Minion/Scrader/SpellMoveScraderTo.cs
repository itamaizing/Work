using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.AI;

public class SpellMoveScraderTo : Skill
{
    [SerializeField] private float _moveDurationPerUnit = 0.2f;
    [SerializeField] private float _damageDelay = 0.5f;
    [SerializeField] private float _attackDistance = 3f;
    [SerializeField] private float _damage = 5f;
    [SerializeField] private Animator _animator;
    [SerializeField] private SkillManager _skillManager;

    private Vector3 _targetPoint = Vector3.positiveInfinity;
    private Coroutine _attackCoroutine;
    private Coroutine _moveWithPathCoroutine;
    private Character _currentEnemyTarget;
    private float _lastAttackTime;
    private Tween _activeTween;
    private bool _moveActive = false;

    private const string _autoAnim = "AutoAttackScrader";

    public Action<GameObject> DoMove;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => !_moveActive;

    private void OnEnable()
    {
        OnSkillCanceled += HandleSkillCanceled;
    }

    private void OnDisable()
    {
        OnSkillCanceled -= HandleSkillCanceled;
        CancelWork();
        Hero.Move.CanMove = true;
    }

    private void HandleSkillCanceled()
    {
        if (_hero?.Move != null)
        {
            Hero.Move.CanMove = true;
            Hero.Move.StopLookAt();
        }

        CancelWork();

        _moveActive = false;
        ClearTarget();
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        _targetPoint = targetInfo.Points[0];
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        Vector3 clickedPoint = Vector3.positiveInfinity;

        while (float.IsPositiveInfinity(clickedPoint.x))
        {
            if (GetMouseButton) clickedPoint = GetMousePoint();
            yield return null;
        }

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Points.Add(clickedPoint);
        callbackDataSaved(targetInfo);
    }
    protected override IEnumerator CastJob()
    {
        _moveActive = true;

        AutoAtackSkillCastWork();

        if (_moveWithPathCoroutine != null)
        {
            StopCoroutine(_moveWithPathCoroutine);
            _moveWithPathCoroutine = null;
        }

        _moveWithPathCoroutine = StartCoroutine(MoveToPointWithNavMeshPath(_targetPoint, false));
        while (_moveActive) yield return null;
    }

    private IEnumerator MoveToPointWithNavMeshPath(Vector3 targetPoint, bool stopAtObstacle)
    {
        Hero.Move.CanMove = false;

        NavMeshPath path = new NavMeshPath();
        bool hasPath = NavMesh.CalculatePath(transform.position, targetPoint, NavMesh.AllAreas, path);

        if (!hasPath || path.status != NavMeshPathStatus.PathComplete)
        {
            Hero.Move.CanMove = true;
            yield break;
        }

        Vector3 lastDoMovePoint = transform.position;

        for (int i = 1; i < path.corners.Length; i++)
        {
            Vector3 segmentTarget = path.corners[i];
            float distance = Vector3.Distance(transform.position, segmentTarget);
            float duration = distance * _moveDurationPerUnit;

            Quaternion lookRotation = Quaternion.LookRotation((segmentTarget - transform.position).normalized);
            transform.rotation = lookRotation;

            bool interruptedByObstacle = false;

            if (_activeTween != null && _activeTween.IsActive())
            {
                _activeTween.Kill();
                _activeTween = null;
            }

            _activeTween = transform.DOMove(segmentTarget, duration)
                .SetEase(Ease.Linear)
                .OnUpdate(() =>
                {
                    if (this == null || !gameObject.activeInHierarchy) return;

                    float movedDist = Vector3.Distance(lastDoMovePoint, transform.position);
                    if (movedDist >= 1f)
                    {
                        DoMove?.Invoke(gameObject);
                        lastDoMovePoint = transform.position;
                    }

                    if (stopAtObstacle && Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 1f, _obstacle)) interruptedByObstacle = true;
                    if (interruptedByObstacle && _activeTween != null && _activeTween.IsActive())
                    {
                        _activeTween.Kill();
                        _activeTween = null;
                    }
                });

            yield return _activeTween.WaitForCompletion();

            if (_activeTween != null && !_activeTween.IsPlaying())
            {
                _activeTween.Kill();
                _activeTween = null;
            }

            if (interruptedByObstacle)
            {
                EndMoveToPointWithNavMeshPath();
                yield break;
            }    
        }

        EndMoveToPointWithNavMeshPath();
    }

    private IEnumerator AttackNearbyEnemiesJob()
    {
        while (true)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, Radius, TargetsLayers);

            Character nearest = null;
            float minDist = float.MaxValue;

            foreach (var hit in hits)
            {
                Character enemy = hit.GetComponent<Character>();
                if (enemy != null && !enemy.IsDead && enemy != Hero)
                {
                    float dist = Vector3.Distance(transform.position, enemy.transform.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearest = enemy;
                    }
                }
            }

            if (nearest == null)
            {
                yield break;
            }

            while (nearest != null && !nearest.IsDead && Vector3.Distance(transform.position, nearest.transform.position) <= Radius)
            {
                float distance = Vector3.Distance(transform.position, nearest.transform.position);

                Vector3 dir = (nearest.transform.position - transform.position).normalized;
                if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);

                if (distance > _attackDistance)
                {
                    Vector3 safeTarget = GetApproachPointNearEnemy(nearest);
                    yield return MoveToPointWithNavMeshPath(safeTarget, false);
                    continue;
                }

                if (Time.time - _lastAttackTime > _damageDelay)
                {
                    _currentEnemyTarget = nearest;
                    _animator.SetTrigger(_autoAnim);
                    _lastAttackTime = Time.time;
                    yield return new WaitForSeconds(_damageDelay);
                }

                if (Input.GetMouseButtonDown(0))
                {
                    _moveActive = false;
                    break;
                }

                yield return null;
            }

            yield return null;
        }
    }

    private Vector3 GetApproachPointNearEnemy(Character enemy)
    {
        Vector3 toEnemy = (enemy.transform.position - transform.position).normalized;
        float stopDistance = _attackDistance - 0.1f;
        return enemy.transform.position - toEnemy * stopDistance;
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

    private void AutoAtackSkillCastWork()
    {
        if (_skillManager != null)
        {
            if (_skillManager.AutoSkillCast != null) _skillManager.AutoSkillCast.DeleteSkill();
        }
    }

    private void EndMoveToPointWithNavMeshPath()
    {

        Hero.Move.CanMove = true;

        if (_attackCoroutine != null)
        {
            StopCoroutine(_attackCoroutine);
            _attackCoroutine = null;
        }

        if (_skillManager.SkillQueue.CurrentSkill.GetType() != typeof(SpellMoveScraderTo) || _skillManager.AutoSkillCast.IsBusy || _skillManager.SkillQueue.IsEmpty == false)
        {
            CancelWork();
            _moveActive = false;
            ClearTarget();
            return;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, Radius, TargetsLayers);

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

        if (nearest != null) _attackCoroutine = StartCoroutine(AttackNearbyEnemiesJob());
        else
        {
            CancelWork();
            _moveActive = false;
            ClearTarget();
        }
    }

    private void CancelWork()
    {
        if (_activeTween != null && _activeTween.IsActive())
        {
            _activeTween.Kill();
            _activeTween = null;
        }

        if (_attackCoroutine != null)
        {
            StopCoroutine(_attackCoroutine);
            _attackCoroutine = null;
        }

        if (_moveWithPathCoroutine != null)
        {
            StopCoroutine(_moveWithPathCoroutine);
            _moveWithPathCoroutine = null;
        }
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
        _targetPoint = Vector3.positiveInfinity;
        _moveActive = false;

        CancelWork();
    }
}