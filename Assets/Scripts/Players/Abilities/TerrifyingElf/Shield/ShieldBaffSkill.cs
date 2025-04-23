using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldBaffSkill : Skill
{
    [SerializeField] private Character _playerLinks;
    private Character _target;

    protected override bool IsCanCast => IsHaveCharge && _target != null;

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;


    public override void LoadTargetData(TargetInfo targetInfo)
    {
        throw new NotImplementedException();
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        while (_target == null)
        {
            if (GetMouseButton)
            {
                _target = GetRaycastTarget(true);
            }
            yield return null;
        }
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Targets.Add(_target);

        targetDataSavedCallback(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (_target != null)
        {
            CmdApplyAbsorptionState(_target.gameObject);
            TryUseCharge();
        }
        yield return null;
    }

    protected override void ClearData()
    {
        _target = null;
    }

    [Command]
    private void CmdApplyAbsorptionState(GameObject targetGameObject)
    {
        var targetCharacter = targetGameObject.GetComponent<Character>();
        if (targetCharacter != null)
        {
            targetCharacter.CharacterState.AddState(States.ShieldBaff, 20, 100, _playerLinks.gameObject, name);
        }
    }
}
