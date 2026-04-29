using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class PortalDarkness : Skill
{
    [SerializeField] private Character _playerLinks;
    [SerializeField] private float _duration = 3f;

    protected override bool IsCanCast => Targeting.GetTarget() != null && Vector3.Distance(Targeting.GetTarget().Transform.position, transform.position) <= AreaInfo.Radius;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();

        while (Targeting.GetTempTarget()?.Targetable == null && !_disactive)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget(Targeting.GetMousePoint(), 0.5f);

                var temp = Targeting.GetTempTarget()?.Targetable as Character;

                if (temp != null)
                {
                    Targeting.SetTarget(temp);
                    break;
                }
            }

            yield return null;
        }

        var target = Targeting.GetTarget()?.Character;

        if (target != null)
        {
            targetInfo.AddTarget(target);
            callbackDataSaved(targetInfo);
        }
    }

    protected override IEnumerator CastJob()
    {
        var target = Targeting.GetTarget()?.Character;

        if (target == null) yield break;

        CmdApplyDarkness(target.gameObject);
        AfterCastJob();
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
    }

    [Command]
    private void CmdApplyDarkness(GameObject targetObject)
    {
        var target = targetObject.GetComponent<Character>();
        if (target == null) return;

        target.CharacterState.AddState(States.PortalDarkness, _duration, 0, _playerLinks.gameObject, name);
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0)
        {
            Targeting.SetTarget((ITargetable)(targetInfo.GetTargets()[0] as Character));
        }
    }
}