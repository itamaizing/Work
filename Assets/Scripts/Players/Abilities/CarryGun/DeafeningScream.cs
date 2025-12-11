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

    private IDamageable _target;

    protected override bool IsCanCast => CheckCanCast();

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("DeafeningScreamAnimation");

    private void OnDisable() => Canceled -= HandleJumpEnd;
    private void OnEnable() => Canceled += HandleJumpEnd;

    private bool CheckCanCast()
    {
        return _target != null && cooldownEnergy.CurrentValue >= jumpWithChelicera.ChargeCooldown &&
        Vector3.Distance(_target.transform.position, transform.position) <= Radius &&
        NoObstacles(_target.transform.position, transform.position, _obstacle);
    }

    public void HandleJumpEnd()
    {
        Hero.Animator.applyRootMotion = false;
        _playerLinks.Move.StopLookAt();
        Hero.Move.CanMove = true;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        ITargetable target = null;

        while (target == null)
        {
            if (GetMouseButton)
            {
                if (GetRaycastTarget() is ITargetable targetable) target = targetable;
            }
            yield return null;
        }

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Targets.Add(target);
        callbackDataSaved.Invoke(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (_target != null) CmdApplyState(_target.gameObject);

        cooldownEnergy.CastCooldownEnergySkill(13, this);
        AfterCastJob();

        yield return null;
    }

    protected override void ClearData()
    {
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

    public void DeafeningScreamAnimationMove()
    {
        if (_hero == null || _hero.Move == null) return;

        _hero.Move.StopMoveAndAnimationMove();
        _hero.Move.CanMove = false;
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
        _isCanCancle = true;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.Targets.Count > 0) _target = targetInfo.Targets[0] as Character;
        if (_target is Character character && character.SelectedCircle != null) character.SelectedCircle.IsActive = false;
        Hero.Move.LookAtTransform(_target.transform);
    }
}
