using DG.Tweening;
using Mirror;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class ScratchClaws : Skill
{
    [SerializeField] private Animator _animator;
    [SerializeField] private Character _playerLinks;
    [SerializeField] private float _moveDurationPerUnit = 0.2f;
    [SerializeField] private float _stopDistance = 1.5f;
    [SerializeField] private float _bleedingDuration = 3f;
    [SerializeField, Range(0, 1f)] private float _bleedingChance = 1f;
    [SerializeField] private float _minDamage = 1f;
    [SerializeField] private float _maxDamage = 4f;

    #region Const
    private const float StopDistanceThreshold = 0.05f;
    private const float MoveEventThreshold = 1f;
    private const float SegmentMinDistance = 0.01f;
    private const float RaycastCheckDistance = 1f;
    private const float TargetSearchRadius = 1f;
    private const float DamagePerTick = 1f;

    private const string AttackScaredTrigger = "AttackScared";

    #endregion
    private IDamageable _currentTarget;
    private Tween _activeTween;
    private bool _setTarget = false;

    public Action<GameObject> DoMove;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    private void scraderClawsAnimCast()
    {
         _animator.SetTrigger(AttackScaredTrigger);
    }

    public void AttackAnimationHit()
    {
        ApplyScratchDamage();
        IsCasting = false;
    }

    protected override bool IsCanCast => !IsCasting;
    private bool IsAllyTarget(IDamageable target) => target.gameObject.layer == TargetsLayers;

    private bool CheckIsCanCast()
    {
        if (GetTarget() == null) return false;
        return Vector3.Distance(GetTarget().Transform.position, transform.position) <= Radius && NoObstacles(GetTarget().Transform.position, transform.position, _obstacle);
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0) SetTarget(targetInfo.GetTargets()[0]);
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
            Hero.Move.CanMove = true;
            Hero.Move.StopLookAt();
        }

        StopCoroutine(MoveToTargetCharacter(_currentTarget));
        IsCasting = false;
        ClearTarget();
        _setTarget = false;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        while (GetTempTarget() == null)
        {
            if (GetMouseButton && !_setTarget)
            {
                FindTarget(TargetSearchRadius, GetMousePoint());

                if (GetTempTarget() != null && GetTempTarget() is IDamageable damageable)
                {
                    _setTarget = true;
                    _currentTarget = damageable;

                    if (IsAllyTarget(damageable) || damageable as Character == Hero) ClearTempTarget();
                    else 
                    {
                        _currentTarget = damageable;
                        break;
                    }
                }
            }

            yield return null;
        }

        SetTarget(GetTempTarget());

        TargetInfo info = new();
        info.AddTarget(GetTarget());
        targetDataSavedCallback?.Invoke(info);
    }

    protected override IEnumerator CastJob()
    {
        IsCasting = true;

        float distanceToTarget = Vector3.Distance(transform.position, GetTarget().Transform.position);
        if (distanceToTarget > _stopDistance + StopDistanceThreshold)
        {
            yield return MoveToTargetCharacter(GetTarget() as IDamageable);
            while (IsCasting) yield return null;
        }

        else
        {
            if (!CheckIsCanCast()) scraderClawsAnimCast();
            while (IsCasting) yield return null;
        }
    }

    protected override void ClearData()
    {
        ClearTarget();

        if (_hero?.Move != null)
        {
            Hero.Move.CanMove = true;
            Hero.Move.StopLookAt();
        }

        IsCasting = false;
        _setTarget = false;
        AnimCastEnded();
    }

    private IEnumerator MoveToTargetCharacter(IDamageable target)
    {
        if (target == null) yield break;

        Hero.Move.LookAtPosition(target.transform.position);

        Vector3 destination = GetApproachPointNearEnemy(target);

        Hero.Move.CanMove = false;

        NavMeshPath path = new NavMeshPath();

        bool hasPath = NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, path);

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

        Hero.Move.CanMove = true;

        if (!IsCasting) yield break;

        scraderClawsAnimCast();
    }
    private Vector3 GetApproachPointNearEnemy(IDamageable enemy)
    {
        Vector3 toEnemy = (enemy.transform.position - transform.position).normalized;
        return enemy.transform.position - toEnemy * _stopDistance;
    }

    private void ApplyScratchDamage()
    {
        if (_currentTarget == null) return;
        Damage = UnityEngine.Random.Range(_minDamage, _maxDamage);

        var targetCurrent = _currentTarget as Character;

        Damage damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(Damage),
            Type = DamageType,
            PhysicAttackType = AttackRangeType
        };

        if (targetCurrent != null && UnityEngine.Random.value <= _bleedingChance) targetCurrent.CharacterState.CmdAddState(States.Bleeding, _bleedingDuration, DamagePerTick, _playerLinks.gameObject, name);
        CmdApplyDamage(damage, targetCurrent.gameObject);
    }
}
