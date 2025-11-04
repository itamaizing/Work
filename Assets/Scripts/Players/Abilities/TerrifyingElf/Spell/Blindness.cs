using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blindness : Skill
{
    [SerializeField] private Character _playerLinks;
    [SerializeField] private float duration;
    private Character _target;

    protected override bool IsCanCast => IsHaveCharge && _target != null;

    protected override int AnimTriggerCastDelay => Animator.StringToHash("SpellCastDelayAnimTrigger");

    protected override int AnimTriggerCast => 0;


    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.Targets.Count > 0) _target = targetInfo.Targets[0] as Character;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        var multiMagic = Hero.CharacterState.GetState(States.MultiMagic) as MultiMagic;

        while (_target == null && !_disactive)
        {
            if (GetMouseButton)
            {
                //_target = GetRaycastTarget(true);
                if (multiMagic != null) multiMagic.LastTarget = _target;
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

            var multiMagic = Hero.CharacterState.GetState(States.MultiMagic) as MultiMagic;

            if (multiMagic != null)
            {
                foreach (var character in multiMagic.PopPendingTargets())
                {
                    TryPayCost();
                    CmdApplyAbsorptionState(character.gameObject);
                }
            }
        }

        AfterCastJob();

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
            targetCharacter.CharacterState.AddState(States.Blind, duration, 0, _playerLinks.gameObject, name);
        }
    }
}
