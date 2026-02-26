using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeafeningScream : Skill
{
    [SerializeField] private Character _playerLinks;
    [SerializeField] private JumpWithChelicera jumpWithChelicera;
    [SerializeField] private CooldownEnergy cooldownEnergy;
    [SerializeField] private float duration = 2f;

    //private IDamageable _target;
    private Character _runtimeTarget;

    protected override bool IsCanCast => CheckCanCast();

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("DeafeningScreamAnimation");

    private void OnDestroy() => Canceled -= HandleJumpEnd;
    private void OnEnable() => Canceled += HandleJumpEnd;

    private bool CheckCanCast()
    {
        return Targeting.GetTarget()?.Character != null && cooldownEnergy.CurrentValue >= jumpWithChelicera.ChargeCooldown &&
        Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position) <= AreaInfo.Radius &&
        Targeting.NoObstacles(Targeting.GetTarget().Character.transform.position, transform.position, _obstacle);
    }

    public void HandleJumpEnd()
    {
        Hero.Animator.applyRootMotion = false;
        _playerLinks.Move.StopLookAt();
        Hero.Move.SetCanMove(true);
        _isCanCancel = true;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        _runtimeTarget = null;

        while (Targeting.GetTarget()?.Character == null)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget();

                if (Targeting.GetTarget()?.Character != null) if (Targeting.GetTarget()?.Character is Character characterTarget) _runtimeTarget = characterTarget;
                _isCanCancel = false;
            }
            yield return null;
        }

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(_runtimeTarget);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (Targeting.GetTarget()?.Character != null) CmdApplyState(Targeting.GetTarget()?.Character.gameObject);

        cooldownEnergy.CastCooldownEnergySkill(13, this);
        AfterCastJob();

        yield return null;
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
        //_target = null;
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

    public void DeafeningScreamAnimationMove()
    {
        if (_hero == null || _hero.Move == null) return;

        _hero.Move.StopMoveAndAnimationMove();
        _hero.Move.SetCanMove(false);
    }

    public void DeafeningScreamCast()
    {
        AnimStartCastCoroutine();
        DeafeningScreamAnimationMove();
        Hero.Animator.applyRootMotion = true;
    }

    public void DeafeningScreamEnd()
    {
        AnimCastEnded();
        HandleJumpEnd();
        _isCanCancel = true;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0) Targeting.SetTarget((ITargetable)(targetInfo.GetTargets()[0] as Character));
        Hero.Move.LookAtTransform(Targeting.GetTarget()?.Character.transform);
        _isCanCancel = false;
    }
}

