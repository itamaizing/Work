using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpBack : Skill
{
    [SerializeField] private float jumpDistance = 1.2f;
    [SerializeField] private float jumpWindow = 1f;

    private Coroutine _jumpWindowCoroutine;

    //private static readonly int jumpStart = Animator.StringToHash("JumpStart");
    //private static readonly int jumpEnd = Animator.StringToHash("JumpEnd");

    //protected override int AnimTriggerCastDelay => jumpStart;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    private Vector3 _mousePosition = Vector3.positiveInfinity;

    protected override bool IsCanCast => true;

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo == null) return;
        if (targetInfo.Targets.Contains(Hero)) return;
        targetInfo.Targets.Add(Hero);
    }

    private void OnDestroy() => Canceled -= HandleJumpEnd;
    private void OnEnable() => Canceled += HandleJumpEnd;

    public void JumpBackaAnimationMove()
    {
        if (_hero == null || _hero.Move == null) return;

        _hero.Move.StopMoveAndAnimationMove();
        _hero.Move.CanMove = false;

        //Vector3 direction = _hero.transform.forward;
        //bool badDirection = float.IsInfinity(_hero.transform.forward.x) || direction.sqrMagnitude < 0.0001f;

        //if (badDirection)
        //{
        //    _hero.Move.StopLookAt();
        //    return;
        //}

        //_hero.Move.LookAtPosition(_hero.transform.forward);
    }

    public void HandleJumpEnd()
    {
        //Hero.Animator.applyRootMotion = false;
        //Hero.Move.StopLookAt();
        Hero.Move.CanMove = true;
        _isCanCancle = true;
    }

    public void JumpCast()
    {
        AnimStartCastCoroutine();
    }

    //public void JumpEnd()
    //{
    //    HandleJumpEnd();
    //    ClearData();
    //    AnimCastEnded();
    //}

    public void ApplyRootJumpBackTrue()
    {
        JumpBackaAnimationMove();
        Hero.Animator.applyRootMotion = true;
    }

    //public void JumpEndSpeedAnim()
    //{
    //    float timeDelay = _jumpDistance / 10;
    //    Hero.Animator.SetFloat("JumpEndSpeed", 1f / timeDelay);
    //}

    //public void HandleJumpAnimEnd()
    //{
    //    if (Hero.Animator != null)
    //    {
    //        float transitionDuration = 0.15f;
    //        Hero.Animator.CrossFade(jumpEnd, transitionDuration);
    //    }
    //}

    public void EnableJumpBack()
    {
        Disactive = false;
        if (_jumpWindowCoroutine != null) StopCoroutine(_jumpWindowCoroutine);
        _jumpWindowCoroutine = StartCoroutine(JumpWindowCoroutine());
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (Disactive && float.IsPositiveInfinity(_mousePosition.x))
        {
            if (GetMouseButton) _mousePosition = GetMousePoint();
            yield return null;
        }


        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Targets.Add(Hero);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        Vector3 jumpDir = -_hero.transform.forward;
        Vector3 targetPos = _hero.transform.position + jumpDir * jumpDistance;

        float duration = jumpDistance / 2f;

        CmdJumpBack(targetPos);
        yield return new WaitForSeconds(duration);
    }

    protected override void ClearData()
    {
        _mousePosition = Vector3.positiveInfinity;
    }

    private IEnumerator JumpWindowCoroutine()
    {
        yield return new WaitForSeconds(jumpWindow);
        Disactive = true;
        _jumpWindowCoroutine = null;
    }

    [Command]
    private void CmdJumpBack(Vector3 targetPos)
    {
        Vector3 from = _hero.transform.position;
        float distance = Vector3.Distance(from, targetPos);
        float speed = 2f;
        float duration = Mathf.Max(0.01f, distance / speed);

        _hero.Move.TargetRpcDoMove(targetPos, duration);
    }
}