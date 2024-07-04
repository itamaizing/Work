using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AutoAttackAbility : TargetAbility
{
    [Header("AutoAttack settings")]
    [SerializeField] private float _attackZoneSize;
    [SerializeField] private float _attackSpeed = 1f;
    [SerializeField] protected LayerMask _obstacle;

    private Coroutine _autoAttackJob;
    private bool _isAttacking = false;

    public float AttackSpeed { get => Buff.AttackSpeed.GetBuffedValue(_attackSpeed); }

    public void Pause()
    {
        if (_autoAttackJob != null)
        {
            StopCoroutine(_autoAttackJob);
            _autoAttackJob = null;
        }
        _isAttacking = false;
    }

    public void Continue()
    {
        if (_autoAttackJob == null && Target != null)
        {
            _autoAttackJob = StartCoroutine(AutoAttackCoroutine());
        }
    }

    protected override void Cleaning()
    {
        base.Cleaning();

        if (_autoAttackJob != null)
        {
            StopCoroutine(_autoAttackJob);
            _autoAttackJob = null;
        }
        IsUsed = false;
        _isAttacking = false;
    }

    private bool NoObstacles()
    {
        var vector = (Target.transform.position - transform.position);
        var dir = vector.normalized;
        float distance = vector.magnitude;

        RaycastHit2D[] rayHit = Physics2D.RaycastAll(transform.position, dir, distance, _obstacle);

        if (rayHit.Length > 0)
            return false;
        else
            return true;
    }

    protected override IEnumerator UseCoroutine()
    {
        yield return _chooseTatgetJob = StartCoroutine(ChooseTargetCoroutine(Radius + 99));
        yield return _autoAttackJob = StartCoroutine(AutoAttackCoroutine());
    }

    protected virtual IEnumerator AutoAttackCoroutine()
    {
        while (Target != null)
        {
            if (IsTargetInRadius(Radius + _attackZoneSize))
            {
                if(IsTargetInRadius(Radius))
                    _isAttacking = true;
                
                if (_isAttacking && NoObstacles())
                {
                    yield return new WaitForSeconds(AttackSpeed);
                    if (IsTargetInRadius(Radius + _attackZoneSize) && NoObstacles() && IsReady)
                    {
                        PayCost(false);
                        CastAction();
                    }
                }
            }
            else
            {
                _isAttacking = false;
            }
            yield return null;
        }
    }
}
