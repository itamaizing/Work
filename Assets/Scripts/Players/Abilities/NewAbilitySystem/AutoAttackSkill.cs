using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AutoAttackSkill : Skill
{
    [Header("AutoAttack settings")]
    [SerializeField] private float _attackZoneSize;
    [SerializeField] protected float _attackSpeed = 1f;

    protected Character _target;
    private bool _isAutoattackMode = true;
    private Coroutine _autoAttackCoroutine;
    private bool _isAttacking = false;
    private Vector2 _lastTargetPosition;

    public float AttackSpeed { get => Buff.AttackSpeed.GetBuffedValue(_attackSpeed); }
    public Character Target { get => _target; }
    public Vector2 LastTargetPosition { get => _lastTargetPosition; }
    public override bool IsPayCostStartCooldown { get => false; }
    public bool IsAutoattackMode { get => _isAutoattackMode; }
    protected override bool IsCanCast
    {
        get
        {
            if (Target == null)
                return false;

            return NoObstacles(Target.transform.position, _obstacle) && IsTargetInRadius(Radius, Target.transform); ;
        }
    }

    public event Action CastPaused;
    public event Action CastContinued;

    protected abstract void CastAction();

    protected override IEnumerator CastJob()
    {
        yield return _autoAttackCoroutine = StartCoroutine(AutoAttackJob());
    }

    protected override void ClearData()
    {
        if (_autoAttackCoroutine != null)
        {
            StopCoroutine(_autoAttackCoroutine);
            _autoAttackCoroutine = null;
        }
        _isAttacking = false;
        _target = null;
    }

    protected override IEnumerator PrepareJob()
    {
        do
        {
            if (GetMouseButton)
            {
                _target = GetRaycastTarget(true);
            }
            yield return null;
        }
        while (Target == null);
    }

    public void SwitchAutoMode()
    {
        _isAutoattackMode = !_isAutoattackMode;
    }

    public void Pause()
    {
        if (_autoAttackCoroutine != null)
        {
            StopCoroutine(_autoAttackCoroutine);
            _autoAttackCoroutine = null;
            CastPaused?.Invoke();
        }
        _isAttacking = false;
    }

    public void Continue()
    {
        if (_autoAttackCoroutine == null && Target != null)
        {
            _autoAttackCoroutine = StartCoroutine(AutoAttackJob());
            CastContinued?.Invoke();
        }
    }

    protected virtual IEnumerator AutoAttackJob()
    {
        while (Target != null)
        {
            if (IsTargetInRadius(Radius + _attackZoneSize, Target.transform))
            {
                if (IsTargetInRadius(Radius, Target.transform))
                    _isAttacking = true;

                if (_isAttacking && NoObstacles(Target.transform.position, _obstacle))
                {
                    _lastTargetPosition = Target.transform.position;
                    
                    if (IsTargetInRadius(Radius + _attackZoneSize, Target.transform) && NoObstacles(Target.transform.position, _obstacle) && IsCooldowned)
                    {
                        if (TryPayCost(true))
                        {
                            CastAction();

                            if (_isAutoattackMode == false)
                            {
                                ClearData();
                                yield break;
                            }

                        }
                    }
                    yield return new WaitForSeconds(AttackSpeed);
                }
            }
            else
            {
                _isAttacking = false;
            }
            yield return null;
        }
        _autoAttackCoroutine = null;
    }
}
