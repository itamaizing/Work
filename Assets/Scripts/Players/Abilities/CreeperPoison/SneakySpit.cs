using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SneakySpit : Skill
{
    [SerializeField] private Character _playerLinks;
    [SerializeField] private float duration = 2f;
    [SerializeField] private float durationErodedArmor = 6f;
    [SerializeField] private float durationWindowsBoost = 2f;
    [SerializeField] private ColdBlood _coldBlood;
    [SerializeField] private CreeperCombo _creeperCombo;

    private Character _attacker;
    private Coroutine _boostWindow;
    private NetworkIdentity identity;
    private bool isAbilityQueue = false;
    private bool isAnimStart = false;
    private bool _isCastControlLocked = false;

    #region Talent

    private bool _isErodedArmorState = false;
    private bool _isColdBloodCrit = false;

    public void ColdBloodStrike(bool value) => _isColdBloodCrit = value;
    public void ErodedArmorState(bool value) => _isErodedArmorState = value;
    #endregion

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
        Targeting.ClearTarget();
    }

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);
        TrySubscribe();
    }

    private void OnEnable()
    {
        TrySubscribe();
        
        isAnimStart = false;
        isAbilityQueue = false;
    }

    private void OnDisable()
    {
        Hero.Health.OnBeforeTakeDamage -= HandleBeforeTakeDamage;
        Hero.Health.Evaded -= OnHeroEvade;
        CastStarted -= HandleCastStarted;
    }

    private void TrySubscribe()
    {
        if (Hero == null)
            return;

        Hero.Health.OnBeforeTakeDamage += HandleBeforeTakeDamage;
        Hero.Health.Evaded += OnHeroEvade;
        CastStarted += HandleCastStarted;
    }

    public void TryStartSneakySpitBoostWindow(Character target)
    {
        TryStartSneakySpitBoostWindow(target, durationWindowsBoost);
    }

    public void TryStartSneakySpitBoostWindow(Character target, float windowDuration)
    {
        if (target == null)
            return;

        if (_boostWindow != null)
        {
            StopCoroutine(_boostWindow);
            _boostWindow = null;
        }

        _boostWindow = StartCoroutine(SneakySpitBoostWindow(target, windowDuration));
    }

    private IEnumerator SneakySpitBoostWindow(Character target, float windowDuration)
    {
        Targeting.SetTarget((ITargetable)target);

        EnableSkillBoost();

        yield return new WaitForSeconds(windowDuration);

        _boostWindow = null;
        FinishBoostWindow();
    }
    
    private void FinishBoostWindow()
    {
        DisableSkillBoost();

        if (!isAnimStart)
        {
            CancelQueuedCast();
        }
    }

    private void CancelQueuedCast()
    {
        if (!isAbilityQueue) return;
        
        TryCancel(true);

        if (_hero.Abilities != null)
            _hero.Abilities.SkillQueue.RemoveNeededSkillFromQueue(this);

        ClearQueueTarget();
        isAbilityQueue = false;
    }

    private void HandleCastStarted()
    {
        isAnimStart = true;
        CancelBoostWindow();
        LockControlDuringCast();
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo?.GetTargets()?.Count > 0)
        {
            if (targetInfo.GetTargets().Count > 0) Targeting.SetTarget((Character)targetInfo.GetTargets()[0]);
            if (Targeting.GetTarget()?.Character != null) Hero.Move.LookAtTransform(Targeting.GetTarget()?.Character.transform);
        }

        _isCanCancel = false;
    }

    private bool CheckCanCast()
    {
        Character target = Targeting.GetTarget()?.Character;

        if (target == null) return false;

        return Vector3.Distance(target.transform.position, transform.position) <= AreaInfo.Radius &&
               Targeting.NoObstacles(target.transform.position, transform.position, _obstacle);
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

    private void DealCriticalDamage(Character target, float baseDamage)
    {
        float critMultiplier = 2.5f;
        float finalDamage = baseDamage * critMultiplier;

        Damage damage = new Damage
        {
            Value = finalDamage,
            School = Info.School,
            Type = Info.DamageType,
        };

        CmdApplyDamage(damage, target.gameObject);
    }

    private void LockControlDuringCast()
    {
        _isCastControlLocked = true;
        Disactive = true;
    }

    private void UnlockControlAfterCast()
    {
        _isCastControlLocked = false;

        if (_boostWindow != null) Disactive = false;
        else Disactive = true;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (isAbilityQueue) yield return null;

        while (Disactive || Targeting.GetTarget()?.Character == null) yield return null;

        Targeting.FindTempTarget();

        isAbilityQueue = true;

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Targeting.GetTarget()?.Character);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        ApplyStateAndDamage();
        yield return null;
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
        Hero.Move.StopLookAt();
        isAbilityQueue = false;
        isAnimStart = false;
        //_target = null;
    }

    public void CancelBoostWindow()
    {
        if (_boostWindow != null)
        {
            StopCoroutine(_boostWindow);
            _boostWindow = null;
            FinishBoostWindow();
        }
    }

    public void ApplyStateAndDamage()
    {
        Character target = Targeting.GetTarget()?.Character;

        if (target != null)
        {
            CmdAddState(target);

            if (_isColdBloodCrit && _coldBlood.IsCanCrit)
            {
                DealCriticalDamage(target, Damage);
                _coldBlood.IsCanCrit = false;
            }

            else
            {
                Damage damage = new Damage
                {
                    Value = Damage,
                    School = Info.School,
                    Type = Info.DamageType,
                };

                CmdApplyDamage(damage, target.gameObject);
            }

            ClearData();
        }
    }

    public void SneakySpitDisactive()
    {
        if (!Disactive) return;
        if (isAnimStart || _isCastControlLocked) return;

        _hero.Animator.SetTrigger(HashAnimPlayer.AnimCancled);
        _hero.NetworkAnimator.SetTrigger(HashAnimPlayer.AnimCancled);

        isAnimStart = false;
        CancelBoostWindow();
        Targeting.ClearTarget();
    }

    private void ConsumeSneakySpitBoost()
    {
        if (_creeperCombo == null) return;
        _creeperCombo.ConsumeSneakySpitBoost();
    }

    public void SneakySpitCast()
    {
        ConsumeSneakySpitBoost();
        AnimStartCastCoroutine();
    }

    public void SneakySpitEnd()
    {
        AnimCastEnded();
        isAnimStart = false;
        UnlockControlAfterCast();
    }

    [Command] 
    private void CmdAddState(Character target)
    {
        GameObject casterObj = Hero != null ? Hero.gameObject : (_playerLinks != null ? _playerLinks.gameObject : gameObject);

        if (_isErodedArmorState) 
            target.CharacterState.AddState(States.ErodedArmor, durationErodedArmor, 0, casterObj, Name);

        target.CharacterState.AddState(States.Blind, duration, 0, casterObj, Name);
    }

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