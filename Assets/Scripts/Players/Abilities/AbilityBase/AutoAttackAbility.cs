using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AutoAttackAbility : TargetAbility
{
    [SerializeField] private float _attackZoneSize;

    private Coroutine _autoAttackJob;

    public void Pause()
    {
        if (_autoAttackJob != null)
        {
            StopCoroutine(_autoAttackJob);
            _autoAttackJob = null;
        }
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
    }

    protected override IEnumerator UseCoroutine()
    {
        yield return _chooseTatgetJob = StartCoroutine(ChooseTatgetCoroutine(Radius));
        yield return _autoAttackJob = StartCoroutine(AutoAttackCoroutine());
    }

    protected virtual IEnumerator AutoAttackCoroutine()
    {
        while (Target != null)
        {
            if (IsTargetInRadius)
            {
                PayCost();
                yield return _castJob = StartCoroutine(CastCoroutine());
            }
            yield return null;
        }
    }
}
