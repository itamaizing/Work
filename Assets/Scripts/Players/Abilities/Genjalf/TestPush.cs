using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestPush : Skill
{
    [SerializeField] private float _pushDistance = 5f;

    private Character _target;

    protected override bool IsCanCast
    {
        get
        {
            if (_target == null)
                return false;

            return NoObstacles(_target.transform.position, _obstacle) && IsTargetInRadius(Radius, _target.transform); ;
        }
    }

    protected override IEnumerator CastJob()
    {
        CmdPush(_target.gameObject);
        yield return null;
    }

    protected override void ClearData()
    {
        _target = null;
    }

    protected override IEnumerator PrepareJob()
    {
        while(_target == null)
        {
            if (Input.GetMouseButton(0))
            {
                _target = GetRaycastTarget(true);
                
            }
            yield return null;
        }
    }

    [Command]
    private void CmdPush(GameObject gameObject)
    {
        gameObject.GetComponent<MoveComponent>().TargetRpcMove();
    }
}
