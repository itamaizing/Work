using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class PowerStrike : Skill
{
    [SerializeField] private Animator _animator;
    [SerializeField] private Character _playerLinks;
    [SerializeField] private float _moveDurationPerUnit = 0.2f;
    [SerializeField] private float _stopDistance = 1.5f;
    [SerializeField] private float _minDamage = 12f;
    [SerializeField] private float _maxDamage = 18f;
    [SerializeField] private float _aoeRadius = 1.5f;

    #region Const
    private const float StopDistanceThreshold = 0.05f;
    private const float MoveEventThreshold = 1f;
    private const float SegmentMinDistance = 0.01f;
    private const float RaycastCheckDistance = 1f;
    private const float TargetSearchRadius = 0.5f;

    private const string AttackGetomirTrigger = "AttackGetomir";
    #endregion

    private IDamageable _currentTarget;
    private Tween _activeTween;
    private Coroutine _moveCoroutine;
    private bool _moveActive = false;

    public Action<GameObject> DoMove;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    private void powerStrikeAnimCast()
    {
        _animator.SetTrigger(AttackGetomirTrigger);
    }

    public void AttackAnimationHit()
    {
        ApplyStrikeDamage();
        _moveActive = false;
    }

    protected override bool IsCanCast => Targeting.GetTarget().Character != null;
    private bool IsAllyTarget(IDamageable target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0) Targeting.SetTarget(targetInfo.GetTargets()[0]);
    }

    private void OnEnable()
    {
        Damage = UnityEngine.Random.Range(_minDamage, _maxDamage);
        OnSkillCanceled += HandleSkillCanceled;
    }

    private void OnDisable()
    {
        OnSkillCanceled -= HandleSkillCanceled;
    }

    private void HandleSkillCanceled()
    {
        if (_hero?.Move != null)
        {
            Hero.Move.SetCanMove(true);
            Hero.Move.StopLookAt();
        }

        _currentTarget = null;
        CancelWork();

        _moveActive = false;
        Targeting.ClearTarget();
        Targeting.ClearTempTarget();
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        while (Targeting.GetTempTarget() == null)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget(Targeting.GetMousePoint(), TargetSearchRadius);

                if (Targeting.GetTempTarget() != null && Targeting.GetTempTarget()?.Damageable is IDamageable damageable)
                {
                    if (IsAllyTarget(damageable) || damageable as Character == Hero) Targeting.ClearTempTarget();
                    else break;
                }
            }

            yield return null;
        }

        Targeting.SetTarget(Targeting.GetTempTarget()?.Character);

        TargetInfo info = new();
        info.AddTarget(Targeting.GetTarget()?.Targetable);
        targetDataSavedCallback?.Invoke(info);
    }

    protected override IEnumerator CastJob()
    {
        CancelWork();
        _moveActive = true;
        _currentTarget = Targeting.GetTarget()?.Character;

        float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.transform.position);
        if (distanceToTarget > _stopDistance + StopDistanceThreshold)
        {
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }

            _moveCoroutine = StartCoroutine(MoveToTargetCharacter(_currentTarget));
            while (_moveActive) yield return null;
        }

        else
        {
            powerStrikeAnimCast();
            while (_moveActive) yield return null;
        }
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
        Targeting.ClearTempTarget();
        _currentTarget = null;

        if (_hero?.Move != null)
        {
            Hero.Move.SetCanMove(true);
            Hero.Move.StopLookAt();
        }

        _moveActive = false;

        CancelWork();
    }

    private IEnumerator MoveToTargetCharacter(IDamageable target)
    {
        if (target == null) yield break;

        Hero.Move.LookAtPosition(target.transform.position);

        Vector3 destination = GetApproachPointNearEnemy(target);

        Hero.Move.SetCanMove(false);

        NavMeshPath path = new NavMeshPath();

        bool hasPath = NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, path);

        if (!hasPath || path.status != NavMeshPathStatus.PathComplete)
        {
            Hero.Move.SetCanMove(true);
            yield break;
        }

        Vector3 lastDoMovePoint = transform.position;

        for (int i = 1; i < path.corners.Length; i++)
        {
            Vector3 segmentTarget = path.corners[i];
            float distance = Vector3.Distance(transform.position, segmentTarget);
            float duration = distance * _moveDurationPerUnit;

            if (distance < SegmentMinDistance) continue;

            bool interrupted = false;

            if (_activeTween != null && _activeTween.IsActive())
            {
                _activeTween.Kill();
                _activeTween = null;
            }

            _activeTween = transform.DOMove(segmentTarget, duration)
                .SetEase(Ease.Linear)
                .OnUpdate(() =>
                {
                    if (!gameObject.activeInHierarchy) return;

                    float movedDist = Vector3.Distance(lastDoMovePoint, transform.position);
                    if (movedDist >= MoveEventThreshold)
                    {
                        DoMove?.Invoke(gameObject);
                        lastDoMovePoint = transform.position;
                    }

                    if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, RaycastCheckDistance, _obstacle)) interrupted = true;

                    if (interrupted)
                    {
                        if (_activeTween != null && _activeTween.IsActive()) _activeTween.Kill();
                    }
                });

            yield return _activeTween.WaitForCompletion();
            if (interrupted) break;
        }

        Hero.Move.SetCanMove(true);

        powerStrikeAnimCast();
    }

    private Vector3 GetApproachPointNearEnemy(IDamageable enemy)
    {
        Vector3 toEnemy = (enemy.transform.position - transform.position).normalized;
        return enemy.transform.position - toEnemy * _stopDistance;
    }

    private void ApplyStrikeDamage()
    {
        if (_currentTarget == null) return;

        Character mainTarget = _currentTarget as Character;
        if (mainTarget == null) return;

        float randomDamage = UnityEngine.Random.Range(_minDamage, _maxDamage);
        float baseDamage = Buff.Damage.GetBuffedValue(randomDamage);

        Vector3 center = mainTarget.transform.position;

        Collider[] hits = Physics.OverlapSphere(center, _aoeRadius, Targeting.Layer);

        foreach (var hit in hits)
        {
            Character character = hit.GetComponent<Character>();
            if (character == null) continue;
            if (!IsValidTarget(character)) continue;

            float finalDamage = character == mainTarget ? baseDamage : baseDamage * 0.5f;

            Damage damage = new Damage
            {
                Value = finalDamage,
                Type = Info.DamageType,
                PhysicAttackType = Info.AttackRangeType
            };

            CmdApplyDamage(damage, character.gameObject);
        }
    }

    private bool IsValidTarget(Character character)
    {
        if (character == Hero) return false;
        if (character.IsDead) return false;

        return true;
    }

    private void CancelWork()
    {
        if (_activeTween != null && _activeTween.IsActive())
        {
            _activeTween.Kill();
            _activeTween = null;
        }

        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
            _moveCoroutine = null;
        }
    }
}
