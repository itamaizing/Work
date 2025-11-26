using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SneakySpit : Skill
{
    [SerializeField] private Character _playerLinks;
    [SerializeField] private float duration = 2f;
    [SerializeField] private float durationWindowsBoost = 2f;

    //private Character _target;
   // private Character _runtimeTarget;
    private Character _attacker;
    private Coroutine _boostWindow;
    private NetworkIdentity identity;

    protected override bool IsCanCast => CheckCanCast();

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("SneakySpitTrigger");

    protected override void SkillEnableBoostLogic()
    {
        //_runtimeTarget = _target;
        Disactive = false;
    }
    protected override void SkillDisableBoostLogic()
    {
        ClearTarget();
        //_runtimeTarget = null;
        Disactive = true;
    }

    private void OnEnable() 
    {
        Hero.Health.OnBeforeTakeDamage += HandleBeforeTakeDamage;
        Hero.Health.Evaded += OnHeroEvade;
    }

    private void OnDisable()
    {
        Hero.Health.OnBeforeTakeDamage -= HandleBeforeTakeDamage;
        Hero.Health.Evaded -= OnHeroEvade;
    }

    public void TryStartSneakySpitBoostWindow(Character target) => _boostWindow = StartCoroutine(SneakySpitBoostWindow(target));

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo?.GetTargets()?.Count > 0)
        {
            SetTarget(targetInfo.GetTargets()[0] as Character);
            if (GetTarget() != null) Hero.Move.LookAtTransform(GetTarget().transform);
        }
        _isCanCancle = false;
    }

    private bool CheckCanCast()
    {
        return GetTarget() != null &&
        Vector3.Distance(GetTarget().transform.position, transform.position) <= Radius &&
        NoObstacles(GetTarget().transform.position, transform.position, _obstacle);
    }

    private void OnHeroEvade()
    {
        if (_attacker == null || _boostWindow != null) return;
        TargetRpcStartSneakySpitBoostWindow(connectionToClient, _attacker.netId);
    }

    private void HandleBeforeTakeDamage(Damage damage, Skill skill)
    {
        if (skill != null && skill.Hero != null) _attacker = skill.Hero;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (Disactive && GetTarget() == null) yield return null;
        FindTarget();
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(GetTarget());
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        ApplyStateAndDamage();
        yield return null;
    }

    private IEnumerator SneakySpitBoostWindow(Character target)
    {
        SetTarget(target);
        if (_boostWindow != null) StopCoroutine(_boostWindow);
        EnableSkillBoost();
        yield return new WaitForSeconds(durationWindowsBoost);
        DisableSkillBoost();
        _boostWindow = null;
    }

    protected override void ClearData()
    {
        ClearTarget();
        //_target = null;
    }

    public void ApplyStateAndDamage()
    {
        if (GetTarget() != null)
        {
            CmdAddState(GetTarget());

            Damage damage = new Damage
            {
                Value = Damage,
                School = School,
                Type = DamageType,
            };

            CmdApplyDamage(damage, GetTarget().gameObject);
            ClearData();
        }
    }

    public void SneakySpitCast() => AnimStartCastCoroutine();
    public void SneakySpitEnd() => AnimCastEnded();

    [Command] private void CmdAddState(Character target) => target.CharacterState.AddState(States.Blind, duration, 0, _playerLinks.gameObject, name);

    [TargetRpc]
    private void TargetRpcStartSneakySpitBoostWindow(NetworkConnection target, uint attackerNetId)
    {
        if (NetworkClient.spawned.TryGetValue(attackerNetId, out NetworkIdentity identity))
        {
            Character attacker = identity.GetComponent<Character>();
            if (attacker != null) TryStartSneakySpitBoostWindow(attacker);
        }
    }
}
