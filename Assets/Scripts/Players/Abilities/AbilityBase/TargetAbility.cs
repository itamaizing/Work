using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TargetAbility : Ability
{
    [Header("Target settings")]
    [SerializeField] protected bool _isCanTargetHimself;

    protected Coroutine _useJob;
    protected Coroutine _castJob;
    protected Coroutine _chooseTatgetJob;
    protected Character _target;

    protected bool IsTarget => (_target.transform == _health.transform);
    protected Character Target => _target;

    protected abstract void CastAction();

    protected override void Cast()
    {
        _useJob = StartCoroutine(UseCoroutine());
    }

    public override bool TryCancel()
    {
        if (base.TryCancel())
        {
            Cleaning();
            return true;
        }
        return false;
    }

    protected bool TryRaycastTarget()
    {
        _target = null;
        RaycastHit2D[] rayHit = Physics2D.RaycastAll(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

        foreach (var item in rayHit)
        {
            if (rayHit.Length > 0 && item.transform.TryGetComponent<Character>(out Character enemy))
            {
                _target = enemy;

                if (_isCanTargetHimself == false && IsTarget)
                {
                    _target = null;
                }
            }
        }
        return _target != null;
    }

    protected bool IsTargetInRadius(float radius)
    {
        if (_target == null)
            return false;

        float distance = Vector3.Distance(_target.transform.position, transform.position);
        return distance <= radius;
    }

    protected virtual void Cleaning()
    {
        _target = null;

        if(_castJob != null)
            StopCoroutine(_castJob);

        if(_useJob != null)
            StopCoroutine(_useJob);

        if(_chooseTatgetJob != null)
            StopCoroutine(_chooseTatgetJob);
    }

    protected virtual IEnumerator UseCoroutine()
    {
        yield return _chooseTatgetJob = StartCoroutine(ChooseTargetCoroutine(Radius));

        if (!PayCost(false))
            yield break;

        yield return GetCastDeleyCoroutine();

        if(_isStreaming == false)
            _isUsed = false;

        CastAction();
    }

    protected virtual IEnumerator ChooseTargetCoroutine(float ChooseRadius)
    {
        _target = null;

        while (_target == null)
        {
            if (Input.GetMouseButtonDown(0) && IsMouseInRadius(ChooseRadius))
            {
                TryRaycastTarget();
            }
            yield return null;
        }
    }
}
