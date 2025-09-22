using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SneakySpit : Skill
{
    [SerializeField] private Character _playerLinks;
    [SerializeField] private float duration = 2f;

    private Character _target;
    private Character _runtimeTarget;

    protected override bool IsCanCast => CheckCanCast();

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("DeafeningScreamAnimation");

    private void OnDestroy() => Canceled -= HandleJumpEnd;
    private void OnEnable() => Canceled += HandleJumpEnd;

    public void HandleJumpEnd()
    {
        Hero.Animator.applyRootMotion = false;
        _playerLinks.Move.StopLookAt();
        Hero.Move.CanMove = true;
        _isCanCancle = true;
    }

    public void SneakySpitAnimationMove()
    {
        if (_hero == null || _hero.Move == null) return;

        _hero.Move.StopMoveAndAnimationMove();
        _hero.Move.CanMove = false;
    }

    public void SneakySpitCast()
    {
        AnimStartCastCoroutine();
        SneakySpitAnimationMove();
        Hero.Animator.applyRootMotion = true;
    }

    public void SneakySpitEnd()
    {
        AnimCastEnded();
        HandleJumpEnd();
        _isCanCancle = true;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.Targets.Count > 0) _target = targetInfo.Targets[0] as Character;
        Hero.Move.LookAtTransform(_target.transform);
        _isCanCancle = false;
    }

    private bool CheckCanCast()
    {
        return _target != null &&
        Vector3.Distance(_target.transform.position, transform.position) <= Radius &&
        NoObstacles(_target.transform.position, transform.position, _obstacle);
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (_target == null)
        {
            if (GetMouseButton)
            {
                _target = GetRaycastTarget();

                if (_target != null)
                {
                    _runtimeTarget = _target;
                    _isCanCancle = false;
                }
            }
            yield return null;
        }

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Targets.Add(_runtimeTarget);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (_target != null) CmdApplyStateAndDamage(_target.gameObject);

        AfterCastJob();

        yield return null;
    }

    protected override void ClearData()
    {
        _target = null;
    }

    [Command]
    private void CmdApplyStateAndDamage(GameObject targetGameObject)
    {
        var targetCharacter = targetGameObject.GetComponent<Character>();
        if (targetCharacter != null)
        {
            targetCharacter.CharacterState.AddState(States.Blind, duration, 0, _playerLinks.gameObject, name);

            Damage damage = new Damage
            {
                Value = Damage,
                School = School,
                Type = DamageType,
            };

            CmdApplyDamage(damage, targetGameObject);
        }
    }
}
