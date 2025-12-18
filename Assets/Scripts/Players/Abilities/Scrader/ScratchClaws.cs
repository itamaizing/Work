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

    private const string AttackScaredMainTrigger = "AttackScaredMain";
    private const string AttackScaredTrigger = "AttackScared";

    #endregion

    private int _attackScaredMainHash = Animator.StringToHash(AttackScaredMainTrigger);
    private Tween _activeTween;
    private bool _moveToTarget = true;
    private bool _setTarget = false;
    private bool _animAttackEnd = false;

    public Action<GameObject> DoMove;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => _attackScaredMainHash;

    public void scraderClawsAnimCast()
    {
        AnimStartCastCoroutine();
    }
    public void scraderClawsAnimCastEnd()
    {
        AnimCastEnded();
    }

    public void scraderClawsAnim()
    {
        _animator.SetTrigger(AttackScaredTrigger);
    }

    public void AnimAttackEnd()
    {
        _animAttackEnd = true;
    }

    protected override bool IsCanCast => GetTarget() != null;
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

        ClearTarget();
        _moveToTarget = true;
        _setTarget = false;
        AnimCastEnded();
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        _moveToTarget = true;
        Damage = UnityEngine.Random.Range(1f, 4f);

        while (GetTempTarget() == null && _moveToTarget)
        {
            if (GetMouseButton && !_setTarget)
            {
                FindTarget(TargetSearchRadius, GetMousePoint());

                if (GetTempTarget() != null && GetTempTarget() is IDamageable damageable)
                {
                    _setTarget = true;

                    if (IsAllyTarget(damageable) || damageable as Character == Hero) ClearTempTarget();
                    else break;
                }
            }

            yield return null;
        }

        SetTarget(GetTempTarget());

        float distanceToTarget = Vector3.Distance(transform.position, GetTempTarget().Transform.position);
        if (distanceToTarget > _stopDistance + StopDistanceThreshold) StartCoroutine(MoveToTargetCharacter(GetTempTarget() as IDamageable));
        else _moveToTarget = false;

        TargetInfo info = new();
        info.AddTarget(GetTarget());
        targetDataSavedCallback?.Invoke(info);
    }

    protected override IEnumerator CastJob()
    {
        if (!CheckIsCanCast()) yield return null;
        _animAttackEnd = false;

        Damage = UnityEngine.Random.Range(1f, 4f);

        IDamageable damageable = GetTarget() as IDamageable;

        while (!_animAttackEnd) yield return null;

        if (!_moveToTarget) CmdApplyScratch(damageable.gameObject);

        yield return null;
    }


    protected override void ClearData()
    {
        ClearTarget();
        _setTarget = false;

        if (_hero?.Move != null)
        {
            Hero.Move.CanMove = true;
            Hero.Move.StopLookAt();
        }

        _moveToTarget = true;
        _setTarget = false;
        AnimCastEnded();
    }

    private IEnumerator MoveToTargetCharacter(IDamageable target)
    {
        if (target == null) yield break;

        _animAttackEnd = false;

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
        _moveToTarget = false;

        _animator.SetTrigger(AttackScaredTrigger);

        while (!_animAttackEnd) yield return null;

        CmdApplyScratch(target.gameObject);
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
        
        if (targetCurrent != null && UnityEngine.Random.value <= _bleedingChance) targetCurrent.CharacterState.AddState(States.Bleeding, _bleedingDuration, DamagePerTick, _playerLinks.gameObject, name);
    }
}
