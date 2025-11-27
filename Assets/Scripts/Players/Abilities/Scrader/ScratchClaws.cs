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
    private bool _moveToTarget = true;
    private bool _setTarget = false;
    public Action<GameObject> DoMove;

    private const string _startAnimTrigger = "AttackScared";

    protected IDamageable _target;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash(_startAnimTrigger);

    protected override bool IsCanCast => _target != null;

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.Targets.Count > 0) _target = targetInfo.Targets[0] as IDamageable;
    }

    public void scraderClawsAnimCast()
    {
        AnimStartCastCoroutine();
    }

    public void scraderClawsAnimCastEnd()
    {
        AnimCastEnded();
    }

    private void OnEnable()
    {
        Damage = UnityEngine.Random.Range(1f, 4f);
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
            _target = null;
            Hero.Move.StopLookAt();
        }

        _moveToTarget = true;
        _setTarget = false;
        AnimCastEnded();
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        ITargetable target = null;

        while (target == null && _moveToTarget)
        {
            if (GetMouseButton && !_setTarget)
            {
                if (GetRaycastTarget() is ITargetable targetable)
                {
                    target = targetable;
                    _setTarget = true;

                    if (target is IDamageable damageable)
                    {
                        float distanceToTarget = Vector3.Distance(transform.position, damageable.transform.position);
                        if (distanceToTarget > _stopDistance + 0.05f) StartCoroutine(MoveToTargetCharacter(damageable));
                        else _moveToTarget = false;
                    }
                }
            }
            yield return null;
        }

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Targets.Add(target);
        targetDataSavedCallback.Invoke(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (_target == null) yield return null;

        Hero.Move.LookAtPosition(_target.transform.position);

        float distance = Vector3.Distance(transform.position, _target.transform.position);
        if (distance <= Radius)
        {
            Damage = UnityEngine.Random.Range(1f, 4f);
            CmdApplyScratch(_target.gameObject);
        }

        yield return null;
    }


    protected override void ClearData()
    {
        _target = null;
        Damage = 0;
        _moveToTarget = true;
        _setTarget = false;
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

            if (distance < 0.01f) continue;

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

        if (target != null)
        {
            Damage = UnityEngine.Random.Range(1f, 4f);
            CmdApplyScratch(target.gameObject);
        }

        Hero.Move.CanMove = true;
        _moveToTarget = false;
    }
    private Vector3 GetApproachPointNearEnemy(IDamageable enemy)
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
