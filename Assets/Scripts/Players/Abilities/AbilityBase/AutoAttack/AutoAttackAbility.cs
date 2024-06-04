using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AutoAttackAbility : TargetAbility
{
    [SerializeField] private float _attackZoneSize;
    [SerializeField] protected float _attackSpeed = 1f;

    private Coroutine _autoAttackJob;
    private bool _isAttacking = false;

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
        if (_autoAttackJob == null)
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

    protected override IEnumerator UseCoroutine()
    {
        yield return _chooseTatgetJob = StartCoroutine(ChooseTatgetCoroutine(Radius + 99));
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
                
                if (_isAttacking)
                {
                    yield return new WaitForSeconds(_attackSpeed);
                    PayCost();
                    IsUsed = true;
                    CastAction();
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
