using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public abstract class AutoAttackSkill : Skill
{
    [Header("AutoAttack settings")]
    [SerializeField] private float _attackManaCost;
    [SerializeField] private float _attackZoneSize;
    [SerializeField] protected float _attackSpeed = 1f;
    [SerializeField] protected LayerMask _obstacle;

    protected bool _isAutoattackMode = true;
    protected Character _target;
    private Coroutine _autoAttackCoroutine;
    private bool _isAttacking = false;

    public float AttackSpeed { get => Buff.AttackSpeed.GetBuffedValue(_attackSpeed); }
    public Character Target { get => _target; }
    protected override bool IsCanCast { get => NoObstacles() && IsTargetInRadius(Radius, Target.transform); }


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
            if (Input.GetMouseButton(0))
            {
                _target = GetRaycastTarget();
            }
            yield return null;
        }
        while (Target == null);
    }

    public void Pause()
    {
        if (_autoAttackCoroutine != null)
        {
            StopCoroutine(_autoAttackCoroutine);
            _autoAttackCoroutine = null;
        }
        _isAttacking = false;
    }

    public void Continue()
    {
        if (_autoAttackCoroutine == null && Target != null)
        {
            _autoAttackCoroutine = StartCoroutine(AutoAttackJob());
        }
    }

    private bool NoObstacles()
    {
        if (Target == null)
            return true;

        var vector = (Target.transform.position - transform.position);
        var dir = vector.normalized;
        float distance = vector.magnitude;

        RaycastHit2D[] rayHit = Physics2D.RaycastAll(transform.position, dir, distance, _obstacle);

        if (rayHit.Length > 0)
            return false;
        else
            return true;
    }

    protected virtual IEnumerator AutoAttackJob()
    {
        while (Target != null)
        {
            if (IsTargetInRadius(Radius + _attackZoneSize, Target.transform))
            {
                if (IsTargetInRadius(Radius, Target.transform))
                    _isAttacking = true;

                if (_isAttacking && NoObstacles())
                {
                    yield return new WaitForSeconds(AttackSpeed);
                    if (IsTargetInRadius(Radius + _attackZoneSize, Target.transform) && NoObstacles() && IsCooldowned)
                    {
                        if (TryPayCost(_attackManaCost))
                        {
                            CastAction();

                            if (_isAutoattackMode == false)
                                ClearData();
                        }
                    }
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
