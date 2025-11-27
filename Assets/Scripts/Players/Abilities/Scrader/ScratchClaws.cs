using DG.Tweening;
using Mirror;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class ScratchClaws : Skill
{
    [SerializeField] private Animator animator;
    [SerializeField] private Character _playerLinks;
    [SerializeField] private float _moveDurationPerUnit = 0.2f;
    [SerializeField] private float _stopDistance = 1.5f;
    [SerializeField] private float _bleedingDuration = 3f;
    [SerializeField, Range(0, 1f)] private float _bleedingChance = 1f;

    private Tween _activeTween;
    public Action<GameObject> DoMove;

    protected IDamageable _target;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    protected override bool IsCanCast => _target != null && !IsCasting;

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.Targets.Count > 0) _target = targetInfo.Targets[0] as IDamageable;
    }

    private void OnEnable()
    {
        Damage = UnityEngine.Random.Range(1f, 4f);
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        ITargetable target = null;

        while (target == null)
        {
            if (GetMouseButton)
            {
                if (GetRaycastTarget() is ITargetable targetable) target = targetable;
            }
            yield return null;
        }

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Targets.Add(target);
        targetDataSavedCallback.Invoke(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (_target == null || _target is not Character targetChar) yield break;

        IsCasting = true;

        yield return MoveToTargetCharacter(targetChar);

        IsCasting = false;
    }

    protected override void ClearData()
    {
        _target = null;
        Damage = 0;
    }

    private IEnumerator MoveToTargetCharacter(Character target)
    {
        if (target == null) yield break;

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

            if (distance < 0.01f) continue;

            Quaternion lookRotation = Quaternion.LookRotation((segmentTarget - transform.position).normalized);
            transform.rotation = lookRotation;

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
                    if (movedDist >= 1f)
                    {
                        DoMove?.Invoke(gameObject);
                        lastDoMovePoint = transform.position;
                    }

                    if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 1f, _obstacle)) interrupted = true;

                    if (interrupted)
                    {
                        if (_activeTween != null && _activeTween.IsActive()) _activeTween.Kill();
                    }
                });

            yield return _activeTween.WaitForCompletion();
            if (interrupted) break;
        }

        Hero.Move.CanMove = true;

        if (target != null)
        {
            animator.SetTrigger("AttackScared");

            Damage = UnityEngine.Random.Range(1f, 4f);
            CmdApplyScratch(_target.gameObject);
        }
    }
    private Vector3 GetApproachPointNearEnemy(Character enemy)
    {
        Vector3 toEnemy = (enemy.transform.position - transform.position).normalized;
        return enemy.transform.position - toEnemy * _stopDistance;
    }

    [Command]
    private void CmdApplyScratch(GameObject target)
    {
        if (target == null) return;

        var targetCurrent = target.GetComponent<Character>();
        
        Damage damage = new Damage
        {
            Value = Damage,
            Type = DamageType.Physical
        };

        ApplyDamage(damage, target);
        if (targetCurrent != null && UnityEngine.Random.value <= _bleedingChance) targetCurrent.CharacterState.AddState(States.BleedingScrader, _bleedingDuration, 1, _playerLinks.gameObject, name);
    }
}
