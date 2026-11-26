using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using SkillSystem.States;
using UnityEngine;

namespace SkillSystem.States
{
    public enum SkillState
    {
        Inactive,
        Preparing,
        Casting,
        Channeling,
        PostCast
    }

    public enum SkillEndReason
    {
        Completed,
        Canceled,
        Interrupted,
        ForcedMiss
    }

    public enum CastRequestResult
    {
        Accepted,
        NotReady,
        InvalidTarget
    }
}

public abstract partial class Skill
{
    #region State

    private SkillState _state = SkillState.Inactive;
    private bool _isTransitioning;
    private readonly Queue<(SkillState to, SkillEndReason reason)> _pendingTransitions = new();

    private bool _isTargeting;
    private bool _castTriggered;
    private bool _cooldownCommitted;
    private bool _isAwaitingCastApproval;
    private CastRequestResult? _castApproval;
    private TargetInfo _pendingQueuedTarget;
    private uint _castRequestId;
    private uint _serverCastRequestId;

    private Coroutine _pipeline;

    public SkillState State => _state;
    public SkillEndReason LastEndReason { get; private set; } = SkillEndReason.Completed;
    public bool LastTransitionCanceled => LastEndReason != SkillEndReason.Completed;

    protected virtual bool StartsCooldownOnInterruptedCast => true;
    public bool IsTargeting => _isTargeting;
    private bool IsIdle => _state == SkillState.Inactive;
    public bool IsExecuting => _isAwaitingCastApproval || !IsIdle;
    public bool HasQueuedTargets => _targetInfoQueue.Count > 0;
    public bool LastCastRequestCanRetry { get; private set; }

    public event Action<SkillState, SkillState> StateChanged;
    public event Action<SkillState> StateExited;
    public event Action<SkillState> StateEntered;
    public event Action CastRejected;

    protected void SetState(SkillState to, SkillEndReason reason = SkillEndReason.Completed)
    {
        if (_isTransitioning)
        {
            _pendingTransitions.Enqueue((to, reason));
            return;
        }

        _isTransitioning = true;
        try
        {
            ApplyTransition(to, reason);
            while (_pendingTransitions.Count > 0)
            {
                var next = _pendingTransitions.Dequeue();
                ApplyTransition(next.to, next.reason);
            }
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    private void ApplyTransition(SkillState to, SkillEndReason reason)
    {
        //Debug.Log($"{_state.ToString()} -> {to.ToString()}");
        var from = _state;
        if (from == to) return;

        _state = to;
        LastEndReason = reason;

        SafeInvoke(() => HandleStateTransition(from, to), $"HandleStateTransition {from}->{to}");
        SafeInvoke(() => StateExited?.Invoke(from), "StateExited");
        SafeInvoke(() => StateEntered?.Invoke(to), "StateEntered");
        SafeInvoke(() => StateChanged?.Invoke(from, to), "StateChanged");
    }

    private void SafeInvoke(Action a, string what)
    {
        try
        {
            a();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Skill:{Name}] {what} threw: {ex}", this);
        }
    }

    protected void EnterChanneling()
    {
        if (_state == SkillState.Casting || _state == SkillState.PostCast)
            SetState(SkillState.Channeling);
    }

    private void EnterPostCast()
    {
        if (_state == SkillState.Casting)
            SetState(SkillState.PostCast);
    }

    #endregion
    
    #region Transition handler

    private void HandleStateTransition(SkillState from, SkillState to)
    {
        if (_hero == null) return;

        switch (to)
        {
            case SkillState.Preparing:
                PlaySound(Sfx_Skill.CastStart);
                HandleMovementLock(MovementLockPhase.CastStarted);
                OnPreparingEntered();
                break;

            case SkillState.Casting:
                if (from == SkillState.Inactive)
                {
                    PlaySound(Sfx_Skill.CastStart);
                    HandleMovementLock(MovementLockPhase.CastStarted);
                }
                OnCastingEntered();
                break;

            case SkillState.Channeling:
                HandleMovementLock(MovementLockPhase.CastTriggered);
                OnChannelingEntered();
                break;

            case SkillState.PostCast:
                HandleMovementLock(MovementLockPhase.CastTriggered);
                OnPostCastEntered();
                break;

            case SkillState.Inactive:
                StopLoopSound(Sfx_Skill.CastLoop);
                if (LastEndReason == SkillEndReason.Completed)
                {
                    PlaySound(Sfx_Skill.CastEnd);
                    if (!_isAutoMode) HandleMovementLock(MovementLockPhase.CastFinished);
                }
                else
                {
                    HandleMovementLock(MovementLockPhase.CastCanceled);
                }
                OnInactiveEntered(LastEndReason);
                break;
        }
    }
    
    protected virtual void OnPreparingEntered() { }
    protected virtual void OnCastingEntered() { }
    protected virtual void OnChannelingEntered() { }
    protected virtual void OnPostCastEntered() { }
    protected virtual void OnInactiveEntered(SkillEndReason reason) { }

    #endregion

    #region Single exit point
    
    private void EndExecution(SkillEndReason reason)
    {
        if (_isAwaitingCastApproval)
        {
            _isAwaitingCastApproval = false;
            StopCoro(ref _pipeline);
            CmdFinishCast(_castRequestId);
            LastEndReason = reason;
            if (!LastCastRequestCanRetry) ConsumePendingQueuedTarget();
            _pendingQueuedTarget = null;
            ResetPhantomCosts();
            ClearData();
            Raise(CastRejected);
            return;
        }

        if (IsIdle) return;
        bool completed = reason == SkillEndReason.Completed;

        StopCoro(ref _pipeline);
        StopCoro(ref _castCoroutine);
        StopCoro(ref _preparingCoroutine);
        StopCoro(ref _channelingCoroutine);

        if (completed || StartsCooldownOnInterruptedCast) CommitCooldown();
        CmdFinishCast(_castRequestId);

        SetState(SkillState.Inactive, reason);

        _castTriggered = false;
        _cooldownCommitted = false;
        _castTimeRollback = 0f;
        _isPlayCastAnim = false;
        _hero.Move.StopLookAt();
        Hero.Abilities.NotifySkillIsTargeting(this, false);
        ClearData();

        if (completed)
        {
            if (Targeting.ForDamage?.Character != null)
            {
                Targeting.ForDamage.Character.SelectedCircle.IsActive = false;
                Targeting.ForDamage.Character.SelectedCircle.SwitchSelectCircle(false);
            }
            Raise(CastFinished);
        }
        else
        {
            ResetPhantomCosts();
            CancelAnim();
            if (reason == SkillEndReason.ForcedMiss) Hero.UIComponent.Miss();
            Raise(Canceled);
            CmdBroadcastCanceled();
        }

        Raise(CastEnded);
        if (!completed) Raise(OnSkillCanceled);
    }
    
    private static void Raise(Action a)
    {
        try { a?.Invoke(); }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private void ResetPhantomCosts()
    {
        foreach (var skillCost in Cost.Values)
            _hero.Resources[skillCost.type].PhantomValueShow(0);
    }

    private void CommitCooldown()
    {
        if (!_castTriggered || _cooldownCommitted) return;
        _cooldownCommitted = true;
        UseCooldownOrCharges();
    }

    private void CommitCooldownOnInterrupt()
    {
        if (_castTriggered && !_cooldownCommitted && StartsCooldownOnInterruptedCast)
            CommitCooldown();
    }
    
    protected void StopCoro(ref Coroutine coroutine)
    {
        if (coroutine != null) StopCoroutine(coroutine);
        coroutine = null;
    }

    #endregion

    #region Cast pipeline
    
    private bool _detachedCastArmed;

    private void StartPipeline()
    {
        var c = StartCoroutine(CastPipeline());
        if (!IsIdle) _pipeline = c;
    }

    private bool ConsumeForceFail()
    {
        if (!_forceFailCastEarly) return false;
        _forceFailCastEarly = false;
        return true;
    }

    private bool ValidateCastTarget()
    {
        var targetable = Targeting.Target?.Targetable;
        if (targetable != null && !IsValidTarget(targetable)) return false;
        return IsCanCast;
    }

    /// <summary>
    /// Prepare → Cast → (Channel) → PostCast → Inactive. Всё в одном месте.
    /// </summary>
    private IEnumerator CastPipeline()
    {
        bool noCast = Hero.Abilities.TryConsumeNoCast();
        bool castFromAnim = !noCast && (AnimTriggerCast != 0 || Animation.CastTriggers.Count > 0);

        SetState(noCast ? SkillState.Casting : SkillState.Preparing);
        Hero.Abilities.NotifySkillPrepared(this);
        Hero.Abilities.NotifySkillIsTargeting(this, true);
        Raise(CastStarted);
        if (IsIdle) yield break;

        if (ConsumeForceFail())
        {
            EndExecution(SkillEndReason.ForcedMiss);
            yield break;
        }

        if (!noCast)
        {
            yield return PreparingJob(PreparingDuration);
            if (IsIdle) yield break;

            if (ConsumeForceFail())
            {
                SpendResources();
                EndExecution(SkillEndReason.ForcedMiss);
                yield break;
            }

            SetState(SkillState.Casting);
        }

        if (castFromAnim)
        {
            _isPlayCastAnim = true;
            PlayCastAnim();
        }
        else
        {
            CancelAnim();
            TriggerCast();
        }

        if (IsIdle) yield break;

        while (!IsIdle)
        {
            if (!_castTriggered)
            {
                if (ConsumeForceFail())
                {
                    EndExecution(SkillEndReason.ForcedMiss);
                    yield break;
                }

                if (!_isPlayCastAnim)
                {
                    Debug.LogWarning($"[Skill:{Name}] анимация каста закончилась без AnimStartCastCoroutine", this);
                    EndExecution(SkillEndReason.Interrupted);
                    yield break;
                }

                //if (!ValidateCastTarget())
                //{
                //    Debug.LogError("Couldn't validate Cast Target");
                //    // Вот тут ложится, если мы используем входя в край радиуса каста
                //    // Пока уберем доп. проверку нахождения в радиусе после начала каста, потом подумаем
                //    EndExecution(SkillEndReason.Interrupted);
                //    yield break;
                //}
            }

            bool waitAnim = castFromAnim && _isPlayCastAnim;
            bool waitJob = _castTriggered && !_cooldownCommitted;
            if (!waitAnim && !waitJob) break;
            yield return null;
        }

        EndExecution(SkillEndReason.Completed);
    }

    private void TriggerCast()
    {
        _castTriggered = true;
        _cooldownCommitted = false;

        SpendResources();
        if (Channeling.CastDuration > 0 && !SkipLegacyChannelingJob)
        {
            var ch = StartCoroutine(ChannelingJob());
            if (IsIdle) return;
            _channelingCoroutine = ch;
        }

        var cast = StartCoroutine(CastJobTracked());
        if (IsIdle) return;
        _castCoroutine = cast;

        Raise(CastSuccess);
        CmdBroadcastCastSuccess();
    }

    private IEnumerator CastJobTracked()
    {
        var job = CastJob();
        while (true)
        {
            object current;
            try
            {
                if (!job.MoveNext()) break;
                current = job.Current;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Skill:{Name}] CastJob threw: {ex}", this);
                break;
            }

            yield return current;
        }
        
        while (_state == SkillState.Channeling && _channelingCoroutine != null) 
            yield return null;

        CommitCooldown();
    }
    
    public void CastDetached(Action trigger)
    {
        _detachedCastArmed = true;
        try { trigger(); }
        finally { _detachedCastArmed = false; }
    }

    [ClientCallback]
    protected void AnimStartCastCoroutine()
    {
        if (!IsExecuting)
        {
            if (!_detachedCastArmed) return;
            StartCoroutine(CastJob());
            return;
        }

        if (_state != SkillState.Casting || _castTriggered)
        {
            Debug.LogWarning($"[Skill:{Name}] AnimStartCastCoroutine проигнорировано в состоянии {State}", this);
            return;
        }

        TriggerCast();
        EnterPostCast();
    }
    
    private void OnAutoModeDisabled()
    {
        ClearQueueTarget();

        if (_isAwaitingCastApproval || _isTargeting || _state == SkillState.Preparing)
            TryCancel();
        else if (!IsExecuting)
            Targeting.ClearTempTarget();
    }

    #endregion

    #region Preparing

    protected Coroutine StartPreparingCoroutine(float time = float.MinValue)
    {
        _preparingCoroutine = StartCoroutine(PreparingJob(time));
        return _preparingCoroutine;
    }

    private IEnumerator PreparingJob(float delayTime)
    {
        NotifyPreparingStarted(delayTime);
        PlayPrepareAnim();
        float time = 0;

        while (time < delayTime)
        {
            if (Targeting.NeedLineOfSight && Targeting.NoObstacles() == false)
            {
                EndExecution(SkillEndReason.Interrupted);
                yield break;
            }

            if (_castTimeRollback > 0f)
            {
                time = Mathf.Max(0f, time - _castTimeRollback);
                _castTimeRollback = 0f;

                float remaining = delayTime - time;
                Animation.SyncSpeedToRemaining(Animation.ActiveClipRawLength, remaining);
            }

            time += Time.deltaTime;
            yield return null;
        }
        
        _preparingCoroutine = null;
        PreparingEnded?.Invoke();
        CmdBroadcastPreparingEnded();
    }

    #endregion

    #region Channeling job

    private IEnumerator ChannelingJob()
    {
        EnterChanneling();
        NotifyChannelingStarted(ChannelingDuration);
        float time = 0;

        while (time < ChannelingDuration)
        {
            if (_castTimeRollback > 0f)
            {
                time += _castTimeRollback;
                _castTimeRollback = 0f;

                float remaining = ChannelingDuration - time;
                Animation.SyncSpeedToRemaining(Animation.ActiveClipRawLength, remaining);
            }

            time += Channeling.TickInterval;

            foreach (var skillCost in Channeling.Costs)
            {
                var resource = _hero.Resources[skillCost.type];
                float cost = Buff.ManaCost.GetBuffedValue(skillCost.value);

                if (resource.CurrentValue < cost)
                {
                    EndExecution(SkillEndReason.Interrupted);
                    yield break;
                }

                resource.CmdUse(cost);
            }

            yield return new WaitForSeconds(Channeling.TickInterval);
        }
        
        _channelingCoroutine = null; 
        EndChanneling();
    }
    
    protected void EndChanneling()
    {
        if (_state == SkillState.Channeling) SetState(SkillState.PostCast);
        Raise(ChannelingEnded);
        CmdBroadcastChannelingEnded();
    }

    #endregion

    #region Cast time rollback

    public void HandleDirectDamageDuringCast(Damage damage)
    {
        if (damage.FullyAbsorbed) return;
        if (!IsExecuting) return;
        if (damage.IsDot) return;

        bool isStream = _state == SkillState.Channeling;
        bool isPrepare = _state == SkillState.Preparing;
        if (!isStream && !isPrepare) return;

        float totalDuration = isStream ? ChannelingDuration : PreparingDuration;

        float rollbackPercent = damage.Type switch
        {
            DamageType.Physical => PhysRollbackBase + damage.Value * RollbackPerDamage,
            DamageType.Magical  => MagRollbackBase + damage.Value * RollbackPerDamage,
            _                   => damage.Value * RollbackPerDamage
        };
        float rollbackAmount = totalDuration * Mathf.Clamp01(rollbackPercent);
        _castTimeRollback += rollbackAmount;

        if (isStream) ChannelingRolledBack?.Invoke(rollbackAmount);
        else PreparingRolledBack?.Invoke(rollbackAmount);
    }

    #endregion

    #region TryCast / TryCancel / Reset

    [Server]
    public virtual bool CheckCast()
    {
        if (Hero == null || Hero.IsDead || !IsCanCast)
            return false;
        if (CooldownEnd > NetworkTime.time)
            return false;
        if (Charges.UsesCharges && !Charges.HasCharges)
            return false;

        return true;
    }
    
    public virtual bool TryCast()
    {
        if (!isClient || !isOwned || IsExecuting) return false;

        LoadTargetDataForCheckCast();
        if (!(IsHaveResources && IsCanCast && !IsExecuting && Hero.IsDead == false))
            return false;

        bool queued = _targetInfoQueue.Count > 0;
        var targetInfo = queued ? _targetInfoQueue.Peek() : CaptureCastTarget();
        RequestCast(targetInfo, queued);
        return true;
    }
    
    public bool TryCast(TargetInfo targetInfo)
    {
        if (!isClient || !isOwned || IsExecuting || targetInfo == null) return false;

        LoadTargetDataForCheckCast();
        if (!(IsHaveResources && !IsExecuting && Hero.IsDead == false))
            return false;

        LoadTargetData(targetInfo);
        if (!IsCanCast) return false;

        RequestCast(targetInfo);
        return true;
    }

    private TargetInfo CaptureCastTarget()
    {
        var info = new TargetInfo();
        var target = Targeting.Target;
        if (target?.Type == TargetType.Object) info.AddTarget(target.Targetable);
        else if (target?.Type == TargetType.Point) info.Points.Add(target.Point);
        return info;
    }

    private void RequestCast(TargetInfo targetInfo, bool queued = false)
    {
        _isAwaitingCastApproval = true;
        _castApproval = null;
        LastCastRequestCanRetry = false;
        _pendingQueuedTarget = queued ? targetInfo : null;
        if (++_castRequestId == 0) ++_castRequestId;

        var targets = targetInfo.GetTargets();
        var targetObjects = new GameObject[targets.Count];
        for (int i = 0; i < targets.Count; i++)
            targetObjects[i] = targets[i].Transform.gameObject;

        CmdTryCast(_castRequestId, targetObjects, targetInfo.Points.ToArray(),
            targetInfo.Roation.ToArray(), targetInfo.SelectedTarget, targetInfo.UsesSelectedTarget);
        _pipeline = StartCoroutine(AwaitCastApproval(targetInfo));
    }

    private IEnumerator AwaitCastApproval(TargetInfo targetInfo)
    {
        yield return null;
        while (!_castApproval.HasValue) yield return null;

        _pipeline = null;
        if (_castApproval.Value != CastRequestResult.Accepted || Hero.IsDead || targetInfo.LostSelectedTarget)
        {
            LastCastRequestCanRetry = _castApproval.Value == CastRequestResult.NotReady &&
                !Hero.IsDead && !targetInfo.LostSelectedTarget;
            EndExecution(SkillEndReason.Interrupted);
            yield break;
        }

        _isAwaitingCastApproval = false;
        ConsumePendingQueuedTarget();
        LoadTargetData(targetInfo);
        LookAtTargetInfo(targetInfo);
        StartPipeline();
    }

    private void ConsumePendingQueuedTarget()
    {
        if (_pendingQueuedTarget != null && _targetInfoQueue.TryPeek(out var head) &&
            ReferenceEquals(head, _pendingQueuedTarget))
            _targetInfoQueue.Dequeue();
        _pendingQueuedTarget = null;
    }

    [Command]
    private void CmdTryCast(uint requestId, GameObject[] targets, Vector3[] points,
        Quaternion[] rotations, Character selectedTarget, bool usesSelectedTarget)
    {
        var result = CastRequestResult.NotReady;
        if (_serverCastRequestId == 0)
        {
            var info = new TargetInfo
            {
                Points = new List<Vector3>(points),
                Roation = new List<Quaternion>(rotations),
                SelectedTarget = selectedTarget,
                UsesSelectedTarget = usesSelectedTarget
            };
            bool validTargets = !info.LostSelectedTarget;
            bool targetAvailable = true;
            foreach (var targetObject in targets)
            {
                var target = targetObject != null ? targetObject.GetComponent<ITargetable>() : null;
                if (target == null || !IsValidTarget(target))
                {
                    validTargets = false;
                    break;
                }
                if (!target.IsTargetable) targetAvailable = false;
                info.AddTarget(target);
            }

            if (!validTargets)
                result = CastRequestResult.InvalidTarget;
            else if (targetAvailable)
            {
                try
                {
                    LoadTargetData(info);
                    result = CheckCast() ? CastRequestResult.Accepted : CastRequestResult.NotReady;
                }
                catch (Exception ex)
                {
                    result = CastRequestResult.InvalidTarget;
                    Debug.LogError($"[Skill:{Name}] Server cast validation threw: {ex}", this);
                }
            }

            if (result == CastRequestResult.Accepted) _serverCastRequestId = requestId;
        }

        TargetCastChecked(connectionToClient, requestId, result);
    }

    [TargetRpc]
    private void TargetCastChecked(NetworkConnectionToClient connection, uint requestId, CastRequestResult result)
    {
        if (_isAwaitingCastApproval && _castRequestId == requestId)
            _castApproval = result;
    }

    [Command]
    private void CmdFinishCast(uint requestId)
    {
        if (_serverCastRequestId == requestId)
            _serverCastRequestId = 0;
    }

    private void LookAtTargetInfo(TargetInfo targetInfo)
    {
        if (targetInfo == null) return;

        var targets = targetInfo.GetTargets();
        if (targets.Count > 0 && targets[0] is Character target)
            _hero.Move.LookAtTransform(target.transform);

        if (targetInfo.Points.Count > 0)
            _hero.Move.LookAtPosition((Vector3)targetInfo.Points[0]);
    }

    public bool TryCancel(bool forceCancel = false)
    {
        if (!forceCancel && _state == SkillState.PostCast) return false;

        if (!forceCancel && !_isCanCancel)
        {
            Hero.Abilities.NotifySkillIsTargeting(this, false);
            return false;
        }

        ResetPhantomCosts();

        bool wasExecuting = IsExecuting;
        bool wasTargeting = _targetingWrapperCoroutine != null;

        if (wasExecuting)
            EndExecution(forceCancel ? SkillEndReason.Interrupted : SkillEndReason.Canceled);

        if (wasTargeting)
            CancelTargeting();

        Targeting.ClearTempTarget();

        if (!wasExecuting)
        {
            _hero.Move.SetCanMove(true);
            ClearData();
            CancelAnim();
            Raise(Canceled);
            CmdBroadcastCanceled();
            Raise(OnSkillCanceled);
        }

        return true;
    }

    public void ResetSkillState()
    {
        EndExecution(SkillEndReason.Interrupted);

        StopCoro(ref _preparingCoroutine);
        PreparingEnded?.Invoke();

        _castTriggered = false;
        _cooldownCommitted = false;
        _isAutoMode = false;

        StopCoro(ref _channelingCoroutine);
        ChannelingEnded?.Invoke();

        StopCoro(ref _targetingWrapperCoroutine);
        StopCoro(ref _targetingCoroutine);
        _isTargeting = false;
        TargetingSession.End(this);
        IsAutoMode = false;

        ClearData();
    }

    #endregion
    
    #region Targeting flow

    public bool TryStartTargeting(object sessionKey = null)
    {
        if (_isTargeting) return false;
        TargetingSession.Begin(this, sessionKey);

        foreach (var skillCost in Cost.Values)
            _hero.Resources[skillCost.type].PhantomValueShow(skillCost.value);

        var c = StartCoroutine(ActionWrapperForTargeting());
        if (_isTargeting) _targetingWrapperCoroutine = c;
        return true;
    }

    private IEnumerator ActionWrapperForTargeting()
    {
        TargetingStarted?.Invoke(this);
        PlaySound(Sfx_Skill.PrepareStart);
        _isTargeting = true;
        Renderer.ShowSmartIndicator();
        if (_informationRenderComponent.IsDynamicRenderer)
            StartDynamicRenderer();

        SubscribeClickEvents();
        _skillRender.SetPrepareCursor();

        yield return _targetingCoroutine = StartCoroutine(TargetingJob(SaveTargetData));

        UnSubscribeClickEvents();
        OnClickCanceled();

        if (_targetInfoQueue.TryPeek(out TargetInfo info) && info.GetTargets().Count > 0
                                                          && info.GetTargets()[0] is Character targetCharacter &&
                                                          targetCharacter != _hero)
        {
            targetCharacter.UIComponent.CircleSelect1.IsActive = false;
        }

        TargetingSuccess?.Invoke(this);
        PlaySound(Sfx_Skill.PrepareEnd);
        Targeting.ClearTempTarget();
        _isTargeting = false;
        TargetingSession.End(this);
        Renderer.HideSmartIndicator();

        _targetingCoroutine = null;
        _targetingWrapperCoroutine = null;
    }

    private void CancelTargeting()
    {
        StopCoro(ref _targetingWrapperCoroutine);
        StopCoro(ref _targetingCoroutine);
        StopCoro(ref _dynamicRendererJob);

        _isTargeting = false;
        Renderer.HideSmartIndicator();

        StopLoopSound(Sfx_Skill.PrepareLoop);
        TargetingCanceled?.Invoke();

        UnSubscribeClickEvents();
        OnClickCanceled();
        
        TargetingSession.End(this);
    }
    
    public void CancelTargetingForNewSession()
    {
        if (!_isTargeting) return;

        if (IsExecuting)
        {
            CancelTargeting();
            Targeting.ClearTempTarget();
            ResetPhantomCosts();
        }
        else TryCancel(true);
    }

    #endregion
}
