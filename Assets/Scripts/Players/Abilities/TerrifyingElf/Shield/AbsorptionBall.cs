using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class AbsorptionBall : Skill
{
    [SerializeField] private Character _playerLinks;
    private Character _target;

    protected override bool IsCanCast => IsHaveCharge && _target != null;

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        _target = (Character)targetInfo.Targets[0];
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (_target == null)
        {
            if (GetMouseButton)
            {
                _target = GetRaycastTarget(true);
            }
            yield return null;
        }
        TargetInfo targetInfo = new();
        targetInfo.Targets.Add(_target);
        callbackDataSaved(targetInfo);
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
            targetCharacter.CharacterState.AddState(States.Absorption, 15, 150, _playerLinks.gameObject, name);
        }
    }
}
