using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class MoveSkill : Skill
{
    [SerializeField] private NavMeshAgent _agent;
    protected override int AnimTriggerCastDelay { get; }
    protected override int AnimTriggerCast { get; }
    private Coroutine _approachRoutine;
    public override void LoadTargetData(TargetInfo targetInfo) { }

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback) { throw new NotImplementedException(); }

    protected override IEnumerator CastJob() { throw new NotImplementedException(); }

    protected override void ClearData() { }

    public void MoveTo()
    {
        if (_approachRoutine != null)
            StopCoroutine(_approachRoutine);

        _approachRoutine = StartCoroutine(MoveRoutine());
    }
    
    public void CancelMove()
    {
        if (_approachRoutine != null)
            StopCoroutine(_approachRoutine);

        _approachRoutine = null;
        
        _agent.SetDestination(_agent.transform.position);
    }

    private IEnumerator MoveRoutine()
    {
        Character target = GetTargetCharacter();
        if (target == null)
            yield break;

        float sqrRadius = _radius * _radius;

        while (true)
        {
            if (target == null)
                yield break;

            Vector3 pos = target.transform.position;
            float sqrDist = (pos - transform.position).sqrMagnitude;

            if (sqrDist <= sqrRadius)
            {
                CancelMove();
                yield break;
            }

            if (!_agent.isOnNavMesh)
                yield break;

            _agent.SetDestination(pos);

            yield return null;
        }
    }
}
