using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeafeningScream : Skill
{
    [SerializeField] private Character _playerLinks;
    [SerializeField] private float duration = 2f;

    private Character _target;
    private Vector3 _targetPoint = Vector3.positiveInfinity;

    protected override bool IsCanCast => IsHaveCharge && _target != null;
    protected override int AnimTriggerCastDelay => Animator.StringToHash("SpellCastDelayAnimTrigger");
    protected override int AnimTriggerCast => 0;

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (float.IsPositiveInfinity(_targetPoint.x) && _target == null && !_disactive)
        {
            if (GetMouseButton)
            {
                _targetPoint = GetMousePoint();
                _target = GetRaycastTarget(true);
            }
            yield return null;
        }

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Targets.Add(_target);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (_target != null) CmdApplyState(_target.gameObject);

        AfterCastJob();

        yield return null;
    }

    protected override void ClearData()
    {
        _targetPoint = Vector3.positiveInfinity;
        _target = null;
    }

    [Command]
    private void CmdApplyState(GameObject targetGameObject)
    {
        var targetCharacter = targetGameObject.GetComponent<Character>();
        if (targetCharacter != null)
        {
            targetCharacter.CharacterState.AddState(States.Stupefaction, duration, 0, _playerLinks.gameObject, name);
        }
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.Targets.Count > 0) _target = targetInfo.Targets[0] as Character;
    }
}
