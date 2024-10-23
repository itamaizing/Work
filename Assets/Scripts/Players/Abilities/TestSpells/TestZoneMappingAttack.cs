using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestZoneMappingAttack : Skill
{
    private Vector3 _targetPoint = Vector3.positiveInfinity;

    protected override bool IsCanCast => true;

    protected override IEnumerator PrepareJob()
    {
        while (float.IsPositiveInfinity(_targetPoint.x))
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 clickedPoint = GetMousePoint();

                if (IsPointInRadius(Radius, clickedPoint))
                {
                    _targetPoint = clickedPoint;
                }
            }
            yield return null;
        }
    }

    protected override IEnumerator CastJob()
    {
        DrawDamageZone(_targetPoint);

        yield return new WaitForSeconds(2f);
        StopDamageZone();
    }

    protected override void ClearData()
    {
        _targetPoint = Vector3.positiveInfinity;
    }
}

