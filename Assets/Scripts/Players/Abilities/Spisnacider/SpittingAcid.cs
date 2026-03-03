using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class SpittingAcid : Skill
{
    [SerializeField] private Animator _animator;
    [SerializeField] private Character _playerLinks;
    [SerializeField] private float _moveDurationPerUnit = 0.2f;
    [SerializeField] private float _stopDistance = 1.5f;
    [SerializeField] private float _minDamage = 1f;
    [SerializeField] private float _maxDamage = 4f;

    [SerializeField] private CreatureCarryGun _creatureCarryGun;

    #region Const
    private const float StopDistanceThreshold = 0.05f;
    private const float MoveEventThreshold = 1f;
    private const float SegmentMinDistance = 0.01f;
    private const float RaycastCheckDistance = 1f;
    private const float TargetSearchRadius = 0.5f;
    private const float CorrodedArmorDuration = 6f;

    private const string AttackSpisnaciderTrigger = "AttackSpisnacider";

    #endregion

    private IDamageable _currentTarget;
    private Tween _activeTween;
    private Coroutine _moveCoroutine;
    private bool _moveActive = false;

    public Action<GameObject> DoMove;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    private void SpisnaciderClawsAnimCast()
    {
        _animator.SetTrigger(AttackSpisnaciderTrigger);
    }

    public void AttackAnimationHit()
    {
        SpittingAcidDamage();
        _moveActive = false;
    }

    protected override bool IsCanCast => GetTarget() != null;
    private bool IsAllyTarget(IDamageable target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");

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
            Hero.Move.SetCanMove(true);
            Hero.Move.StopLookAt();
        }

        _currentTarget = null;
        CancelWork();

        _moveActive = false;
        ClearTarget();
        ClearTempTarget();
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        while (GetTempTarget() == null)
        {
            if (GetMouseButton)
            {
                FindTarget(TargetSearchRadius, GetMousePoint());

                if (GetTempTarget() != null && GetTempTarget() is IDamageable damageable)
                {
                    if (IsAllyTarget(damageable) || damageable as Character == Hero) ClearTempTarget();
                    else break;
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
        CancelWork();
        _moveActive = true;
        _currentTarget = GetTarget() as Character;

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
            SpisnaciderClawsAnimCast();
            while (_moveActive) yield return null;
        }
    }

    protected override void ClearData()
    {
        ClearTarget();
        ClearTempTarget();
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

        SpisnaciderClawsAnimCast();
    }

    private Vector3 GetApproachPointNearEnemy(IDamageable enemy)
    {
        Vector3 toEnemy = (enemy.transform.position - transform.position).normalized;
        return enemy.transform.position - toEnemy * _stopDistance;
    }

    private void SpittingAcidDamage()
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

        CmdApplyDamage(damage, targetCurrent.gameObject);
        targetCurrent.CharacterState.CmdAddState(States.CorrodedArmor, CorrodedArmorDuration, 0f, Hero.gameObject, Name);    
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

