using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class Impatica : Skill
{
    [SerializeField] private Character _playerLinks;
    [SerializeField] private float duration;
    private Vector3 _targetPoint = Vector3.positiveInfinity;

    protected override bool IsCanCast => IsHaveCharge && GetTargetCharacter() != null;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (float.IsPositiveInfinity(_targetPoint.x) && GetTargetCharacter() == null && !_disactive)
        {
            if (GetMouseButton)
            {
                _targetPoint = GetMousePoint();
                FindTargetCharacter();
            }
            yield return null;
        }

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(GetTargetCharacter());
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (GetTargetCharacter() != null)
        {
            CmdApplyImpaticaState(GetTargetCharacter().gameObject);

        }

        yield return null;
    }

    protected override void ClearData()
    {
        _targetPoint = Vector3.positiveInfinity;
        ClearTarget();
        //_target = null;
    }

    [Command]
    private void CmdApplyImpaticaState(GameObject targetGameObject)
    {
        var targetCharacter = targetGameObject.GetComponent<Character>();
        if (targetCharacter != null)
        {
            targetCharacter.CharacterState.AddState(States.Impatience, duration, 0, _playerLinks.gameObject, name);
        }
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0) SetTarget((ITargetable)(targetInfo.GetTargets()[0] as Character));
    }
}