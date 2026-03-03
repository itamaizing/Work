using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public abstract class SkillCreatureCarryGun : Skill
{
    [Header("Common Settings")]
    [SerializeField] protected Animator animator;
    [SerializeField] protected float moveDurationPerUnit = 0.2f;
    [SerializeField] protected float stopDistance = 1.5f;
    [SerializeField] protected float targetSearchRadius = 0.5f;

    protected IDamageable currentTarget;
    protected Tween activeTween;
    protected Coroutine moveCoroutine;
    protected bool moveActive;

    protected const float StopDistanceThreshold = 0.05f;
    protected const float SegmentMinDistance = 0.01f;
    protected const float RaycastCheckDistance = 1f;
    protected const float MoveEventThreshold = 1f;

    public Action<GameObject> DoMove;

    protected abstract string AnimationTrigger { get; }
    protected abstract void ApplySkillEffect(Character target);

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    protected override bool IsCanCast => GetTarget() != null;

    private bool IsAllyTarget(IDamageable target)
        => target.gameObject.layer == LayerMask.NameToLayer("Allies");

    #region Target

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0)
            SetTarget(targetInfo.GetTargets()[0]);
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callback)
    {
        while (GetTempTarget() == null)
        {
            if (GetMouseButton)
            {
                FindTarget(targetSearchRadius, GetMousePoint());

                if (GetTempTarget() is IDamageable dmg)
                {
                    if (IsAllyTarget(dmg) || dmg as Character == Hero)
                        ClearTempTarget();
                    else
                        break;
                }
            }

            yield return null;
        }

        SetTarget(GetTempTarget());

        TargetInfo info = new();
        info.AddTarget(GetTarget());
        callback?.Invoke(info);
    }

    #endregion

    #region Cast

    protected override IEnumerator CastJob()
    {
        CancelWork();
        moveActive = true;
        currentTarget = GetTarget() as Character;

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

        if (distance > stopDistance + StopDistanceThreshold)
        {
            moveCoroutine = StartCoroutine(MoveToTarget(currentTarget));
            while (moveActive) yield return null;
        }
        else
        {
            TriggerAnimation();
            while (moveActive) yield return null;
        }
    }

    protected void TriggerAnimation()
    {
        animator.SetTrigger(AnimationTrigger);
    }

    public void AnimationHit()
    {
        if (currentTarget is Character ch)
            ApplySkillEffect(ch);

        moveActive = false;
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
            float duration = dist * moveDurationPerUnit;

            if (dist < SegmentMinDistance) continue;

            bool interrupted = false;

            activeTween?.Kill();

            activeTween = transform.DOMove(segment, duration)
                .SetEase(Ease.Linear)
                .OnUpdate(() =>
                {
                    float moved = Vector3.Distance(lastMovePoint, transform.position);
                    if (moved >= MoveEventThreshold)
                    {
                        DoMove?.Invoke(gameObject);
                        lastMovePoint = transform.position;
                    }

                    if (Physics.Raycast(transform.position, transform.forward, RaycastCheckDistance, _obstacle))
                        interrupted = true;

                    if (interrupted)
                        activeTween?.Kill();
                });

            yield return activeTween.WaitForCompletion();
            if (interrupted) break;
        }

        Hero.Move.SetCanMove(true);
        TriggerAnimation();
    }

    private Vector3 GetApproachPoint(IDamageable enemy)
    {
        Vector3 dir = (enemy.transform.position - transform.position).normalized;
        return enemy.transform.position - dir * stopDistance;
    }

    #endregion

    #region Cleanup

    protected override void ClearData()
    {
        ClearTarget();
        ClearTempTarget();
        currentTarget = null;

        Hero?.Move?.SetCanMove(true);
        Hero?.Move?.StopLookAt();

        moveActive = false;
        CancelWork();
    }

    private void CancelWork()
    {
        activeTween?.Kill();

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
    }

    #endregion
}