using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestPush : TargetAbility
{
    [SerializeField] private float _pushDistance = 5f;

    private Coroutine _pushJob;

    protected override void Cancel()
    {
        CmdCancle();
    }

    protected override void CastAction()
    {
        CmdPush(Target.transform);
    }

    private IEnumerator PushCoroutine(Transform targetTransform, Vector3 targetPosition)
    {
        float time = 0;

        while (StreamingDuration > time)
        {
            targetTransform.position = Vector2.MoveTowards(targetTransform.position, targetPosition, (_pushDistance * Time.deltaTime) / StreamingDuration);

            time += Time.deltaTime;
            yield return null;
        }
    }

    [Command]
    private void CmdPush(Transform targetTransform)
    {
        Vector3 dir = (targetTransform.position - transform.position).normalized * _pushDistance;
        dir += targetTransform.position;
        _pushJob = StartCoroutine(PushCoroutine(targetTransform, dir));
    }

    [Command]
    private void CmdCancle()
    {
        if (_pushJob != null)
            StopCoroutine(_pushJob);
    }
}
