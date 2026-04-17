using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class DischargingSkill : MoveSkill
{
    [SerializeField] private float _dischargeDuration = 6f;
    protected override bool IsCanCast
    {
        get => CheckCanCast();
    }

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("DischargeSkill");
    private bool IsEnemyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Enemy");

    private float _clickRadius = 0.5f;
    private float _particleLifetime = 1f;

    private void OnEnable()
    {
        Canceled += CancelMove;
    }

    private void OnDisable()
    {
        Canceled -= CancelMove;
    }

    private bool CheckCanCast()
    {
        return Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position) <=
               AreaInfo.Radius;
    }

    public void AnimCastDischarge()
    {
        AnimStartCastCoroutine();
    }

    public void AnimEndDischarge()
    {
        AnimCastEnded();
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        Targeting.SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
        if (!IsCanCast)
        {
            MoveTo();
        }
    }

    protected override IEnumerator CastJob()
    {
        if (Targeting.GetTarget()?.Character != null)
        {
            var target = Targeting.GetTarget()?.Character;
            CmdAddState(target.gameObject);
        }

        yield return null;
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();
        while (Targeting.GetTempTarget() == null)
        {
            if (GetMouseButton)
            {
                Vector3 clickPoint = Targeting.GetMousePoint();

                Targeting.FindTempTarget(clickPoint, _clickRadius, canTargetSelf: false);
                if (Targeting.GetTempTarget()?.Character is Character character)
                {
                    if (Targeting.GetTempTarget()?.Character != null && !IsEnemyTarget(character))
                    {
                        Targeting.ClearTempTarget();
                    }
                    else
                    {
                        if (character.SelectedCircle != null) character.SelectedCircle.IsActive = false;
                        break;
                    }
                }
            }

            yield return null;
        }

        targetInfo.AddTarget(Targeting.GetTempTarget()?.Character);
        Targeting.ClearTempTarget();
        callbackDataSaved(targetInfo);
    }

    [Command]
    private void CmdAddState(GameObject target)
    {
        if (target.TryGetComponent(out Character character))
        {
            character.CharacterState.AddState(States.Discharge, _dischargeDuration, 0, Schools.Air, Hero.gameObject, name);
        }
    }
}
