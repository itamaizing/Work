using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TargetOrAreaAbility : TargetAbility
{
    private Vector3 _point;

    public Vector3 Point => _point;

    protected override void Cleaning()
    {
        base.Cleaning();
        _point = Vector3.zero;
    }

    protected override IEnumerator ChooseTargetCoroutine(float ChooseRadius)
    {
        _target = null;
        _point = Vector3.zero;

        while (Target == null && _point == Vector3.zero)
        {
            if (Input.GetMouseButtonDown(0) && IsMouseInRadius(ChooseRadius))
            {
                TryRaycastTarget();
                _point = GetMousePoint();
            }
            yield return null;
        }
    }
}
