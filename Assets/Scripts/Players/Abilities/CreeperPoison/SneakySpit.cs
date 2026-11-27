using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class SneakySpit : Skill
{
    [SerializeField] private Character _playerLinks;
    [SerializeField] private float duration = 2f;
    [SerializeField] private float durationErodedArmor = 6f;
    [SerializeField] private float durationWindowsBoost = 2f;
    [SerializeField] private ColdBlood _coldBlood;
    [SerializeField] private CreeperCombo _creeperCombo;

    private Coroutine _boostWindow;
    private Character _serverBoostTarget;
    private double _serverBoostWindowEnd;
    private bool _subscribed;
    private bool isAbilityQueue = false;

    #region Talent

    private bool _isErodedArmorState = false;
    public void ErodedArmorState(bool value) => _isErodedArmorState = value;
    #endregion

    protected override bool IsCanCast => CheckCanCast();

    protected override int AnimTriggerPrepare => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("SneakySpitTrigger");

    protected override void SkillEnableBoostLogic()
    {
        Disactive = false;
    }
    protected override void SkillDisableBoostLogic()
    {
        Disactive = true;
        if (IsExecuting) return;
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
    }

    private void OnDisable()
    {
        _serverBoostTarget = null;
        _serverBoostWindowEnd = 0;
        CancelBoostWindow();
        if (!_subscribed) return;

        Hero.Health.Evaded -= OnHeroEvade;
        CastStarted -= HandleCastStarted;
        _subscribed = false;
    }

    private void TrySubscribe()
    {
        if (_subscribed || !isActiveAndEnabled || Hero == null)
            return;

        if (_creeperCombo == null) _creeperCombo = GetComponent<CreeperCombo>();

        Hero.Health.Evaded += OnHeroEvade;
        CastStarted += HandleCastStarted;
        _subscribed = true;
    }

    public void TryStartSneakySpitBoostWindow(Character target)
    {
        TryStartSneakySpitBoostWindow(target, durationWindowsBoost);
    }

    [Server]
    public void ServerStartSneakySpitBoostWindow(Character target, float windowDuration)
    {
        if (target == null || target.IsDead || !IsSkillActive || !isActiveAndEnabled) return;

        _serverBoostTarget = target;
        _serverBoostWindowEnd = NetworkTime.time + windowDuration;
    }

    public void TryStartSneakySpitBoostWindow(Character target, float windowDuration)
    {
        if (target == null || target.IsDead || !IsSkillActive || !isActiveAndEnabled || IsExecuting)
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

        if (!IsExecuting)
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
        CancelBoostWindow();
        if (_creeperCombo != null) _creeperCombo.ConsumeSneakySpitBoost();
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
        
        if (isServer)
        {
            if (target != _serverBoostTarget || NetworkTime.time >= _serverBoostWindowEnd) return false;
        }
        else if (_boostWindow == null || Disactive) return false;

        return Vector3.Distance(target.transform.position, transform.position) <= AreaInfo.Radius &&
               Targeting.NoObstacles(target.transform.position, transform.position, Targeting.AdditionalObstacle);
    }

    private void OnHeroEvade(Skill skill)
    {
        Character attacker = skill?.Hero;
        if (!isServer || attacker == null || connectionToClient == null) return;

        ServerStartSneakySpitBoostWindow(attacker, durationWindowsBoost);
        TargetRpcStartSneakySpitBoostWindow(connectionToClient, attacker.netId);
    }

    private void DealCriticalDamage(Character target, float baseDamage)
    {
        float critMultiplier = 2.5f;
        float finalDamage = baseDamage * critMultiplier;

        Damage damage = GetDamage();
        damage.Value = finalDamage;

        CmdApplyDamage(damage, target.gameObject);
    }

    protected override IEnumerator TargetingJob(Action<TargetInfo> callbackDataSaved)
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
            CmdApplySpitStates(target.gameObject);

            if (_coldBlood != null && _coldBlood.TryConsumeForPoison(target))
            {
                DealCriticalDamage(target, Damage);
            }
            else
            {
                Damage damage = GetDamage();
                CmdApplyDamage(damage, target.gameObject);
            }
        }
    }

    public void SneakySpitDisactive()
    {
        if (!isOwned || !Disactive || IsExecuting) return;

        _hero.Animator.SetTrigger(HashAnimPlayer.AnimCancled);
        _hero.NetworkAnimator.SetTrigger(HashAnimPlayer.AnimCancled);

        CancelBoostWindow();
        Targeting.ClearTarget();
    }

    public void SneakySpitCast()
    {
        AnimStartCastCoroutine();
    }

    public void SneakySpitEnd()
    {
        AnimCastEnded();
    }

    [Command]
    private void CmdApplySpitStates(GameObject targetObject)
    {
        if (targetObject == null || !targetObject.TryGetComponent<Character>(out var target) || target.IsDead) return;

        _serverBoostTarget = null;
        _serverBoostWindowEnd = 0;

        GameObject casterObj = Hero != null ? Hero.gameObject : (_playerLinks != null ? _playerLinks.gameObject : gameObject);

        target.CharacterState.AddState(States.Blind, duration, 0, casterObj, Name);

        if (_isErodedArmorState)
            target.CharacterState.AddState(States.ErodedArmor, durationErodedArmor, 0, casterObj, Name);
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
