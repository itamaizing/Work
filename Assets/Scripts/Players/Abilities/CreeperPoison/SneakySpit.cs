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

    private Character _attacker;
    private Coroutine _boostWindow;
    private NetworkIdentity identity;
    private bool isAbilityQueue = false;
    private bool isAnimStart = false;

    protected override bool IsCanCast => CheckCanCast();

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("SneakySpitTrigger");

    protected override void SkillEnableBoostLogic()
    {
        Disactive = false;
    }
    protected override void SkillDisableBoostLogic()
    {
        Disactive = true;
        if (isAnimStart) return;
        ClearTarget();
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
            if (targetInfo.GetTargets().Count > 0) SetTarget((Character)targetInfo.GetTargets()[0]);
            if (GetTargetCharacter() != null) Hero.Move.LookAtTransform(GetTargetCharacter().transform);
        }
        _isCanCancle = false;
    }

    private bool CheckCanCast()
    {
        return GetTargetCharacter() != null &&
        Vector3.Distance(GetTargetCharacter().transform.position, transform.position) <= Radius &&
        NoObstacles(GetTargetCharacter().transform.position, transform.position, _obstacle);
    }

    private void OnHeroEvade()
    {
        Debug.Log($"_attacker: {_attacker}");
        if (_attacker == null || _boostWindow != null) return;

        TargetRpcStartSneakySpitBoostWindow(connectionToClient, _attacker.netId);
    }

    private void HandleBeforeTakeDamage(Damage damage, Skill skill)
    {
        if (skill != null && skill.Hero != null) _attacker = skill.Hero;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (isAbilityQueue) yield return null;
        while (Disactive || GetTargetCharacter() == null) yield return null;

        FindTargetCharacter();
        isAbilityQueue = true;
        isAnimStart = true;

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(GetTargetCharacter());
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        ApplyStateAndDamage();
        yield return null;
    }

    private IEnumerator SneakySpitBoostWindow(Character target)
    {
        SetTarget((ITargetable)target);
        if (_boostWindow != null) StopCoroutine(_boostWindow);
        EnableSkillBoost();
        yield return new WaitForSeconds(durationWindowsBoost);
        DisableSkillBoost();
        _boostWindow = null;
    }

    protected override void ClearData()
    {
        ClearTarget();
        Hero.Move.StopLookAt();
        isAbilityQueue = false;
        //_target = null;
    }
    
    public void CancelBoostWindow()
    {
        if (_boostWindow != null)
        {
            StopCoroutine(_boostWindow);
            _boostWindow = null;
            DisableSkillBoost();
        }
    }

    public void ApplyStateAndDamage()
    {
        if (GetTargetCharacter() != null)
        {
            CmdAddState(GetTargetCharacter());

            Damage damage = new Damage
            {
                Value = Damage,
                School = School,
                Type = DamageType,
            };

            CmdApplyDamage(damage, GetTargetCharacter().gameObject);
            ClearData();
        }
    }

    public void SneakySpitCast() => AnimStartCastCoroutine();

    public void SneakySpitEnd()
    {
        AnimCastEnded();
        isAnimStart = false;
        CancelBoostWindow();
    }

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
