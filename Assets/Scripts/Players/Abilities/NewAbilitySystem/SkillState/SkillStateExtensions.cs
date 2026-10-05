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

    private Coroutine _pipeline;

    public SkillState State => _state;
    public SkillEndReason LastEndReason { get; private set; } = SkillEndReason.Completed;
    public bool LastTransitionCanceled => LastEndReason != SkillEndReason.Completed;

    protected virtual bool StartsCooldownOnInterruptedCast => true;
    public bool IsTargeting => _isTargeting;
    private bool IsIdle => _state == SkillState.Inactive;
    public bool IsExecuting => !IsIdle;
    public bool HasQueuedTargets => _targetInfoQueue.Count > 0;

    public event Action<SkillState, SkillState> StateChanged;
    public event Action<SkillState> StateExited;
    public event Action<SkillState> StateEntered;

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
        if (IsIdle) return;
        bool completed = reason == SkillEndReason.Completed;

        StopCoro(ref _pipeline);
        StopCoro(ref _castCoroutine);
        StopCoro(ref _preparingCoroutine);
        StopCoro(ref _channelingCoroutine);

        if (completed || StartsCooldownOnInterruptedCast) CommitCooldown();

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
        var damageable = Targeting.ForDamage?.Damageable;
        if (damageable != null && !IsValidTarget(damageable)) return false;
        return IsCanCast;
    }

    /// <summary>
    /// Prepare → Cast → (Channel) → PostCast → Inactive. Всё в одном месте.
    /// </summary>
    private IEnumerator CastPipeline()
    {
        bool noCast = Hero.Abilities.TryConsumeNoCast();
        bool castFromAnim = !noCast && AnimTriggerCast != 0;

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

                if (!ValidateCastTarget())
                {
                    EndExecution(SkillEndReason.Interrupted);
                    yield break;
                }
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

    [ClientCallback]
    protected void AnimStartCastCoroutine()
    {
        if (_state != SkillState.Casting || _castTriggered)
        {
            if (IsExecuting)
                Debug.LogWarning($"[Skill:{Name}] AnimStartCastCoroutine проигнорировано в состоянии {State}",
                    this);
            return;
        }

        TriggerCast();
        EnterPostCast();
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
        PreparingStarted?.Invoke(delayTime);
        CmdBroadcastPreparingStarted(delayTime);
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
        ChannelingStarted?.Invoke(ChannelingDuration);
        CmdBroadcastChannelingStarted(ChannelingDuration);
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

    public void HandleDirectDamageDuringCast(float damageValue, DamageType type, bool fullyAbsorbed)
    {
        if (fullyAbsorbed) return;
        if (!IsExecuting) return;

        bool isStream = _state == SkillState.Channeling;
        bool isPrepare = _state == SkillState.Preparing;
        if (!isStream && !isPrepare) return;

        float totalDuration = isStream ? ChannelingDuration : PreparingDuration;

        float rollbackPercent = type == DamageType.Physical
            ? PhysRollbackBase + damageValue * RollbackPerDamage
            : MagRollbackBase + damageValue * RollbackPerDamage;

        float rollbackAmount = totalDuration * Mathf.Clamp01(rollbackPercent);
        _castTimeRollback += rollbackAmount;

        if (isStream) ChannelingRolledBack?.Invoke(rollbackAmount);
        else PreparingRolledBack?.Invoke(rollbackAmount);
    }

    #endregion

    #region TryCast / TryCancel / Reset

    public virtual bool TryCast()
    {
        if (IsExecuting) return false;

        LoadTargetDataForCheckCast();
        if (!(IsHaveResources && IsCanCast && !IsExecuting && Hero.IsDead == false))
            return false;

        if (_targetInfoQueue.Count > 0)
        {
            var targetInfo = _targetInfoQueue.Dequeue();
            LoadTargetData(targetInfo);
            LookAtTargetInfo(targetInfo);
        }

        StartPipeline();
        return true;
    }

    public bool TryCast(TargetInfo targetInfo)
    {
        if (IsExecuting) return false;

        LoadTargetDataForCheckCast();
        if (!(IsHaveResources && !IsExecuting && Hero.IsDead == false))
            return false;

        LoadTargetData(targetInfo);
        if (!IsCanCast) return false;

        StartPipeline();
        LookAtTargetInfo(targetInfo);
        return true;
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

        ClearData();
    }

    #endregion
    
    #region Targeting flow

    public bool TryStartTargeting()
    {
        if (_isTargeting) return false;

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
    }

    #endregion
}