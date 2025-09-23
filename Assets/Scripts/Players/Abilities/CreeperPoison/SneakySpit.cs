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
    private Coroutine _boostWindow;

    protected override bool IsCanCast => CheckCanCast();

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    protected override void SkillEnableBoostLogic()
    {
        _runtimeTarget = _target;
        Disactive = false;
    }
    protected override void SkillDisableBoostLogic()
    {
        _runtimeTarget = null;
        Disactive = true;
    }

    private void OnEnable() 
    {
        Hero.Health.Evaded += OnHeroEvade;
    }

    private void OnDisable()
    {
        Hero.Health.Evaded -= OnHeroEvade;
    }

    public void TryStartSneakySpitBoostWindow(Character target) => _boostWindow = StartCoroutine(SneakySpitBoostWindow(target));

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo?.Targets?.Count > 0)
        {
            _target = targetInfo.Targets[0] as Character;
            if (_target != null) Hero.Move.LookAtTransform(_target.transform);
        }
        _isCanCancle = false;
    }

    private bool CheckCanCast()
    {
        return _target != null &&
        Vector3.Distance(_target.transform.position, transform.position) <= Radius &&
        NoObstacles(_target.transform.position, transform.position, _obstacle);
    }

    private void OnHeroEvade()
    {
        if (_target == null || _boostWindow != null) return;

        TryStartSneakySpitBoostWindow(_target);
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (Disactive && _target == null) yield return null;

        _hero.NetworkAnimator.SetTrigger(Animator.StringToHash("SneakySpitTrigger"));
        _hero.Animator.SetTrigger(Animator.StringToHash("SneakySpitTrigger"));

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Targets.Add(_runtimeTarget);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        yield return null;
    }

    private IEnumerator SneakySpitBoostWindow(Character target)
    {
        _target = target;
        if (_boostWindow != null) StopCoroutine(_boostWindow);
        EnableSkillBoost();
        yield return new WaitForSeconds(2f);
        DisableSkillBoost();
        _boostWindow = null;
    }

    protected override void ClearData()
    {
        _target = null;
    }

    public void ApplyStateAndDamage()
    {
        if (_runtimeTarget != null)
        {
            CmdAddState(_runtimeTarget);

            Damage damage = new Damage
            {
                Value = Damage,
                School = School,
                Type = DamageType,
            };

            CmdApplyDamage(damage, _runtimeTarget.gameObject);
            ClearData();
        }
    }

    [Command] private void CmdAddState(Character target) => target.CharacterState.AddState(States.Blind, duration, 0, _playerLinks.gameObject, name);
}
