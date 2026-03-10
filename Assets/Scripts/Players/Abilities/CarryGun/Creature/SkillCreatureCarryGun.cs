using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public abstract class SkillCreatureCarryGun : Skill
{
    [Header("Modifier Settings")]
    [SerializeField] protected MoveCreature _moveCreature;

    [Header("Common Settings")]
    [SerializeField] protected Animator _animator;
    [SerializeField] protected float _stopDistance = 1.5f;

    protected IDamageable _currentTarget;
    protected Tween _activeTween;
    protected Coroutine _moveCoroutine;
    protected bool _moveActive;

    protected const float StopDistanceThreshold = 0.05f;
    protected const float SegmentMinDistance = 0.01f;
    protected const float RaycastCheckDistance = 1f;
    protected const float MoveEventThreshold = 1f;
    protected const float TargetSearchRadius = 0.5f;

    public Action<GameObject> DoMove;

    protected abstract string AnimationTrigger { get; }
    protected abstract void ApplySkillEffect(Character target);

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    protected override bool IsCanCast => Targeting.GetTarget() != null;

    private bool IsAllyTarget(IDamageable target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");

    #region Target

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0) Targeting.SetTarget(targetInfo.GetTargets()[0]);
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callback)
    {
        while (Targeting.GetTempTarget() == null)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget(Targeting.GetMousePoint(), TargetSearchRadius);

                if (Targeting.GetTempTarget() is IDamageable damageable)
                {
                    if (IsAllyTarget(damageable) || damageable as Character == Hero) Targeting.ClearTempTarget();
                    else break;
                }
            }

            yield return null;
        }

        Targeting.SetTarget(Targeting.GetTempTarget()?.Character);

        TargetInfo info = new();
        info.AddTarget(Targeting.GetTarget()?.Character);
        callback?.Invoke(info);
    }

    #endregion

    #region Cast

    protected override IEnumerator CastJob()
    {
        CancelWork();
        _moveActive = true;
        _currentTarget = Targeting.GetTarget()?.Character;

        float distance = Vector3.Distance(transform.position, _currentTarget.transform.position);

        if (distance > _stopDistance + StopDistanceThreshold)
        {
            _moveCoroutine = StartCoroutine(MoveToTarget(_currentTarget));
            while (_moveActive) yield return null;
        }
        else
        {
            TriggerAnimation();
            while (_moveActive) yield return null;
        }
    }

    public void TriggerAnimation()
    {
        float speed = 1f;

        if (_moveCreature != null) speed = _moveCreature.SpeedModifier;

        _animator.speed = speed;
        _animator.SetTrigger(AnimationTrigger);
    }

    public void AnimationHit()
    {
        if (_currentTarget is Character character) ApplySkillEffect(character);
        _moveActive = false;
    }

    #endregion

    #region Movement

    private IEnumerator MoveToTarget(IDamageable target)
    {
        if (target == null) yield break;

        Hero.Move.LookAtPosition(target.transform.position);

        Vector3 destination = GetApproachPoint(target);
        Hero.Move.SetCanMove(false);

        NavMeshPath path = new();
        bool hasPath = NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, path);

        if (!hasPath || path.status != NavMeshPathStatus.PathComplete)
        {
            Hero.Move.SetCanMove(true);
            yield break;
        }

        Vector3 lastMovePoint = transform.position;

        for (int i = 1; i < path.corners.Length; i++)
        {
            Vector3 segment = path.corners[i];
            float dist = Vector3.Distance(transform.position, segment);
            float duration = dist * _moveCreature.MoveDurationPerUnit;

            if (dist < SegmentMinDistance) continue;

            bool interrupted = false;

            _activeTween?.Kill();

            _activeTween = transform.DOMove(segment, duration)
                .SetEase(Ease.Linear)
                .OnUpdate(() =>
                {
                    float moved = Vector3.Distance(lastMovePoint, transform.position);
                    if (moved >= MoveEventThreshold)
                    {
                        DoMove?.Invoke(gameObject);
                        lastMovePoint = transform.position;
                    }

                    if (Physics.Raycast(transform.position, transform.forward, RaycastCheckDistance, _obstacle)) interrupted = true;

                    if (interrupted) _activeTween?.Kill();
                });

            yield return _activeTween.WaitForCompletion();
            if (interrupted) break;
        }

        Hero.Move.SetCanMove(true);
        TriggerAnimation();
    }

    private Vector3 GetApproachPoint(IDamageable enemy)
    {
        Vector3 dir = (enemy.transform.position - transform.position).normalized;
        return enemy.transform.position - dir * _stopDistance;
    }

    #endregion

    #region Cleanup

    protected override void ClearData()
    {
        Targeting.ClearTarget();
        Targeting.ClearTempTarget();
        _currentTarget = null;

        Hero?.Move?.SetCanMove(true);
        Hero?.Move?.StopLookAt();

        _moveActive = false;

        if (_animator != null) _animator.speed = 1f;

        CancelWork();
    }

    private void CancelWork()
    {
        _activeTween?.Kill();

        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
            _moveCoroutine = null;
        }
    }

    #endregion
}