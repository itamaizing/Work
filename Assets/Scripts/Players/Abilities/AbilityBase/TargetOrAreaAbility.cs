using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TargetOrAreaAbility : TargetAbility
{
    private Vector3 _point;

    public Vector3 Point => _point;

    protected override IEnumerator ChooseTatgetCoroutine(float ChooseRadius)
    {
        while (Target == null && _point == null)
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
