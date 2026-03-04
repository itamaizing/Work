using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blindness : Skill
{
    [SerializeField] private Character _playerLinks;
    [SerializeField] private float duration;
    //private Character _target;

    protected override bool IsCanCast => IsHaveCharge && Targeting.GetTarget()?.Character != null;

    protected override int AnimTriggerCastDelay => Animator.StringToHash("SpellCastDelayAnimTrigger");

    protected override int AnimTriggerCast => 0;


    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0) Targeting.SetTarget((ITargetable)(targetInfo.GetTargets()[0] as Character));
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        var multiMagic = Hero.CharacterState.GetState(States.MultiMagic) as MultiMagic;

        while (Targeting.GetTarget()?.Character == null && !_disactive)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget();
                //_target = GetRaycastTarget(true);
                if (multiMagic != null) multiMagic.LastTarget = Targeting.GetTarget()?.Character;
            }
            yield return null;
        }
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Targeting.GetTarget()?.Character);

        targetDataSavedCallback(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (Targeting.GetTarget()?.Character != null)
        {
            CmdApplyAbsorptionState(Targeting.GetTarget()?.Character.gameObject);

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
        Targeting.ClearTarget();
        //_target = null;
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
