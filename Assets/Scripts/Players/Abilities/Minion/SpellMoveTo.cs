using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SpellMoveTo : Skill
{
    [SerializeField] private NavMeshAgent _agent;
    private Vector3 _targetPoint = Vector3.positiveInfinity;

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;

    protected override bool IsCanCast => true;

    protected override IEnumerator CastJob()
    {
        _agent.SetDestination(_targetPoint);
        yield return null;
    }

    protected override void ClearData()
    {
        _targetPoint = Vector3.positiveInfinity;
    }

    protected override IEnumerator PrepareJob()
    {
        while (float.IsPositiveInfinity(_targetPoint.x))
        {
            if (GetMouseButton)
            {
                _targetPoint = GetMousePoint();
            }
            yield return null;
        }
    }
}
