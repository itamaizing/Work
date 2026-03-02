using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;
using UnityEngine.AI;

public abstract class SpellMoveCreatureTo : Skill
{
    [Header("Movement")]
    [SerializeField] protected float moveDurationPerUnit = 0.2f;
    [SerializeField] protected float attackDistance = 3f;
    [SerializeField] protected float damageDelay = 0.5f;

    [Header("Refs")]
    [SerializeField] protected Animator animator;
    [SerializeField] protected SkillManager skillManager;

    protected Vector3 targetPoint = Vector3.positiveInfinity;

    protected Coroutine attackCoroutine;
    protected Coroutine moveCoroutine;
    protected Character currentEnemyTarget;
    protected float lastAttackTime;
    protected Tween activeTween;
    protected bool moveActive;

    public Action<GameObject> DoMove;

    protected abstract string AutoAttackTrigger { get; }
    protected abstract void DealDamage(Character target);

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => !moveActive;

    #region Cast Flow

    protected void ClearAutoAttackSkill()
    {
        if (skillManager == null) return;

        if (skillManager.AutoSkillCast != null) skillManager.AutoSkillCast.DeleteSkill();
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callback)
    {
        Vector3 clicked = Vector3.positiveInfinity;

        while (float.IsPositiveInfinity(clicked.x))
        {
            if (GetMouseButton)
                clicked = Targeting.GetMousePoint();

            yield return null;
        }

        TargetInfo info = new TargetInfo();
        info.Points.Add(clicked);
        callback(info);
    }

    public override void LoadTargetData(TargetInfo info)
    {
        targetPoint = info.Points[0];
    }

    protected override IEnumerator CastJob()
    {
        ClearAutoAttackSkill();
        moveActive = true;

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(MoveWithPath(targetPoint, false));

        while (moveActive)
            yield return null;
    }

    #endregion

    #region Movement

    protected IEnumerator MoveWithPath(Vector3 point, bool stopAtObstacle)
    {
        Hero.Move.SetCanMove(false);

        NavMeshPath path = new NavMeshPath();
        bool hasPath = NavMesh.CalculatePath(transform.position, point, NavMesh.AllAreas, path);

        if (!hasPath || path.status != NavMeshPathStatus.PathComplete)
        {
            Hero.Move.SetCanMove(true);
            yield break;
        }

        Vector3 lastDoMovePoint = transform.position;

        for (int i = 1; i < path.corners.Length; i++)
        {
            Vector3 segment = path.corners[i];
            float distance = Vector3.Distance(transform.position, segment);
            float speed = Mathf.Max(0.01f, moveDurationPerUnit);
            float duration = distance / speed;

            transform.rotation = Quaternion.LookRotation((segment - transform.position).normalized);

            bool interrupted = false;

            activeTween?.Kill();

            activeTween = transform.DOMove(segment, duration)
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

                    if (stopAtObstacle &&
                        Physics.Raycast(transform.position, transform.forward,
                        out RaycastHit hit, 1f, _obstacle))
                    {
                        interrupted = true;
                        activeTween?.Kill();
                    }
                });

            yield return activeTween.WaitForCompletion();

            if (interrupted)
            {
                EndMovement();
                yield break;
            }
        }

        EndMovement();
    }

    #endregion

    #region Combat

    private void EndMovement()
    {
        Hero.Move.SetCanMove(true);

        if (attackCoroutine != null) StopCoroutine(attackCoroutine);

        if (skillManager != null)
        {
            if (skillManager.SkillQueue.IsEmpty == false ||
                skillManager.AutoSkillCast.IsBusy)
            {
                StopSkill();
                return;
            }
        }

        Character nearest = FindNearestEnemy();

        if (nearest != null) attackCoroutine = StartCoroutine(AutoAttackLoop());
        else StopSkill();
    }

    private IEnumerator AutoAttackLoop()
    {
        while (true)
        {
            if (skillManager != null)
            {
                if (skillManager.SkillQueue.IsEmpty == false ||
                    skillManager.AutoSkillCast.IsBusy)
                {
                    StopSkill();
                    yield break;
                }
            }

            Character nearest = FindNearestEnemy();
            if (nearest == null) yield break;

            float distance = Vector3.Distance(transform.position, nearest.transform.position);

            Vector3 dir = (nearest.transform.position - transform.position).normalized;
            if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);

            if (distance > attackDistance)
            {
                yield return MoveWithPath(GetApproachPoint(nearest), false);
                continue;
            }

            if (Time.time - lastAttackTime > damageDelay)
            {
                currentEnemyTarget = nearest;
                animator.SetTrigger(AutoAttackTrigger);
                lastAttackTime = Time.time;

                yield return new WaitForSeconds(damageDelay);
            }

            yield return null;
        }
    }

    protected Character FindNearestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, AreaInfo.Radius, Targeting.Layer);

        Character nearest = null;
        float minDist = float.MaxValue;

        foreach (var hit in hits)
        {
            Character enemy = hit.GetComponent<Character>();
            if (enemy == null || enemy.IsDead || enemy == Hero) continue;

            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = enemy;
            }
        }

        return nearest;
    }

    protected Vector3 GetApproachPoint(Character enemy)
    {
        Vector3 dir = (enemy.transform.position - transform.position).normalized;
        return enemy.transform.position - dir * (attackDistance - 0.1f);
    }

    public void OnAutoAttackAnimationHit()
    {
        if (currentEnemyTarget != null) DealDamage(currentEnemyTarget);
    }

    public void OnAutoAttackAnimationEnd()
    {
        currentEnemyTarget = null;
    }

    #endregion

    protected void StopSkill()
    {
        activeTween?.Kill();
        moveActive = false;
        Targeting.ClearTarget();
    }

    protected override void ClearData()
    {
        targetPoint = Vector3.positiveInfinity;
        moveActive = false;
        activeTween?.Kill();
    }
}