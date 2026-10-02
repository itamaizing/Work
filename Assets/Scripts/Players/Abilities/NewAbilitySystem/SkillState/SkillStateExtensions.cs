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
        FailedEarly
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
    private bool _runActive;
    private int _runId;
    private bool _preparingActive;
    private bool _channelingActive;

    public SkillState State => _state;
    public SkillEndReason LastEndReason { get; private set; } = SkillEndReason.Completed;
    public bool LastTransitionCanceled => LastEndReason != SkillEndReason.Completed;

    protected virtual bool StartsCooldownOnInterruptedCast => true;
    public bool IsTargeting => _isTargeting;
    public bool IsExecuting => _runActive || _state != SkillState.Inactive;
    public bool HasQueuedTargets => _targetInfoQueue.Count > 0;
    
    protected bool IsChannelingRunning => _channelingActive || IsCustomChannelingActive;

    public event Action<SkillState, SkillState> StateChanged;
    public event Action<SkillState> StateExited;
    public event Action<SkillState> StateEntered;
    
    protected void SetState(SkillState to, bool canceled)
        => SetState(to, canceled ? SkillEndReason.Canceled : SkillEndReason.Completed);

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

    protected virtual void HandleStateTransition(SkillState from, SkillState to)
    {
        if (_hero == null) return;

        if (from == SkillState.Inactive)
        {
            PlaySound(Sfx_Skill.CastStart);
            HandleMovementLock(MovementLockPhase.CastStarted);
        }
        else if ((to == SkillState.Channeling || to == SkillState.PostCast)
                 && (from == SkillState.Preparing || from == SkillState.Casting))
        {
            HandleMovementLock(MovementLockPhase.CastTriggered);
        }

        if (to == SkillState.Inactive)
        {
            switch (LastEndReason)
            {
                case SkillEndReason.Completed:
                    PlaySound(Sfx_Skill.CastEnd);
                    if (!_isAutoMode) HandleMovementLock(MovementLockPhase.CastFinished);
                    break;

                case SkillEndReason.FailedEarly:
                    StopLoopSound(Sfx_Skill.CastLoop);
                    HandleMovementLock(MovementLockPhase.CastFailedEarly);
                    break;

                default:
                    StopLoopSound(Sfx_Skill.CastLoop);
                    HandleMovementLock(MovementLockPhase.CastCanceled);
                    break;
            }
        }
    }

    #endregion

    #region Single exit point
    
    public void Interrupt(SkillEndReason reason)
    {
        FinishExecution(reason, stopPipeline: true);
    }
    
    private void FinishExecution(SkillEndReason reason, bool stopPipeline)
    {
        if (!_runActive) return;

        _runActive = false;
        _runId++;

        if (stopPipeline) StopCoro(ref _pipeline);
        else _pipeline = null;
        StopCoro(ref _castCoroutine);
        StopCoro(ref _preparingCoroutine);
        StopCoro(ref _channelingCoroutine);
        _preparingActive = false;
        _channelingActive = false;

        SetState(SkillState.Inactive, reason);
        OnExecutionFinished(reason);
    }

    private void OnExecutionFinished(SkillEndReason reason)
    {
        bool completed = reason == SkillEndReason.Completed;
        
        if (completed) CommitCooldown();
        else CommitCooldownOnInterrupt();
        _castTriggered = false;
        _cooldownCommitted = false;
        _castTimeRollback = 0f;
        _isPlayCastAnim = false;

        if (!completed) ResetPhantomCosts();

        Hero.Abilities.NotifySkillIsTargeting(this, false);
        _hero.Move.StopLookAt();

        if (completed && Targeting.ForDamage != null && Targeting.ForDamage.Character != null)
        {
            Targeting.ForDamage.Character.SelectedCircle.IsActive = false;
            Targeting.ForDamage.Character.SelectedCircle.SwitchSelectCircle(false);
        }

        ClearData();
        if (!completed) CancelAnim();
        if (reason == SkillEndReason.FailedEarly) Hero.UIComponent.Miss();

        if (!completed)
        {
            SafeInvoke(() => Canceled?.Invoke(), "Canceled");
            CmdBroadcastCanceled();
        }

        if (completed) SafeInvoke(() => CastFinished?.Invoke(), "CastFinished");
        SafeInvoke(() => CastEnded?.Invoke(), "CastEnded");
        if (!completed) SafeInvoke(() => OnSkillCanceled?.Invoke(), "OnSkillCanceled");
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
        if (_runActive) _pipeline = c;
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
    /// Выход из любой точки: reason = ...; yield break; — finally гарантирует переход в Inactive.
    /// </summary>
    private IEnumerator CastPipeline()
    {
        _runActive = true;
        int run = ++_runId;
        var reason = SkillEndReason.Completed;

        try
        {
            if (ConsumeForceFail())
            {
                reason = SkillEndReason.FailedEarly;
                yield break;
            }

            bool noCast = Hero.Abilities.TryConsumeNoCast();
            bool prepare = !noCast;
            bool castAnim = !noCast && AnimTriggerCast != 0;

            // ── вход ──
            SetState(prepare ? SkillState.Preparing : SkillState.Casting);
            Hero.Abilities.NotifySkillPrepared(this);
            Hero.Abilities.NotifySkillIsTargeting(this, true);
            SafeInvoke(() => CastStarted?.Invoke(), "CastStarted");
            if (run != _runId) yield break; // подписчик мог нас прервать

            // ── Preparing ──
            if (prepare)
            {
                yield return PreparingJob(PreparingDuration);
                if (run != _runId) yield break;
                if (ConsumeForceFail())
                {
                    reason = SkillEndReason.FailedEarly;
                    yield break;
                }
            }

            // ── Casting ──
            SetState(SkillState.Casting);
            if (castAnim)
            {
                _isPlayCastAnim = true;
                PlayCastAnim();
            }
            else
            {
                CancelAnim();
                TriggerCast();
                EnterPostCast();
            }

            if (run != _runId) yield break;

            // ── Ожидание: событие каста / поток / CastJob / остаточная анимация ──
            while (true)
            {
                if (!_castTriggered)
                {
                    if (ConsumeForceFail())
                    {
                        reason = SkillEndReason.FailedEarly;
                        yield break;
                    }

                    if (!_isPlayCastAnim)
                    {
                        Debug.LogWarning(
                            $"[Skill:{Name}] Анимация каста закончилась без события AnimStartCastCoroutine — каст не сработал",
                            this);
                        reason = SkillEndReason.FailedEarly;
                        yield break;
                    }

                    if (!ValidateCastTarget())
                    {
                        reason = SkillEndReason.Interrupted;
                        yield break;
                    }
                }
                else if (_state == SkillState.Channeling && !IsChannelingRunning)
                {
                    SetState(SkillState.PostCast); // поток закончился
                }

                bool animBusy = castAnim && _isPlayCastAnim;
                bool jobBusy = _castTriggered && !_cooldownCommitted;
                if (!animBusy && !jobBusy) break;

                yield return null;
                if (run != _runId) yield break;
            }
        }
        finally
        {
            if (run == _runId) FinishExecution(reason, stopPipeline: false);
        }
    }

    private void TriggerCast()
    {
        _castTriggered = true;
        _cooldownCommitted = false;

        SpendResources();
        if (_castDuration > 0 && !SkipLegacyChannelingJob)
        {
            var ch = StartCoroutine(ChannelingJob());
            if (!_runActive) return;
            _channelingCoroutine = _channelingActive ? ch : null;
        }

        var cast = StartCoroutine(CastJobTracked());
        if (!_runActive) return;
        _castCoroutine = cast;

        SafeInvoke(() => CastSuccess?.Invoke(), "CastSuccess");
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
        
        while (IsChannelingRunning)
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
        if (time == float.MinValue)
            time = PreparingDuration;

        var c = StartCoroutine(PreparingJob(time));
        _preparingCoroutine = _preparingActive ? c : null;
        return _preparingCoroutine;
    }

    private IEnumerator PreparingJob(float delayTime)
    {
        _preparingActive = true;
        PreparingStarted?.Invoke(delayTime);
        CmdBroadcastPreparingStarted(delayTime);
        PlayPrepareAnim();
        float time = 0;

        while (time < delayTime)
        {
            if (Targeting.NeedLineOfSight && Targeting.NoObstacles() == false)
            {
                Interrupt(SkillEndReason.Interrupted);
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

        _preparingActive = false;
        _preparingCoroutine = null;
        PreparingEnded?.Invoke();
        CmdBroadcastPreparingEnded();
    }

    #endregion

    #region Channeling job

    private IEnumerator ChannelingJob()
    {
        _channelingActive = true;
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

            time += _manaCostRate;

            foreach (var skillCost in _manaCostPerTick)
            {
                var resource = _hero.Resources[skillCost.type];
                float cost = Buff.ManaCost.GetBuffedValue(skillCost.value);

                if (resource.CurrentValue < cost)
                {
                    Interrupt(SkillEndReason.Interrupted);
                    yield break;
                }

                resource.CmdUse(cost);
            }

            yield return new WaitForSeconds(_manaCostRate);
        }

        _channelingActive = false;
        _channelingCoroutine = null;
        ChannelingEnded?.Invoke();
        CmdBroadcastChannelingEnded();
    }

    #endregion

    #region Cast time rollback

    public void HandleDirectDamageDuringCast(float damageValue, DamageType type, bool fullyAbsorbed)
    {
        if (fullyAbsorbed) return;
        if (!IsExecuting) return;

        bool isStream = _state == SkillState.Channeling || IsCustomChannelingActive;
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

        bool wasExecuting = _runActive;
        bool wasTargeting = _targetingWrapperCoroutine != null;

        if (wasExecuting)
            Interrupt(forceCancel ? SkillEndReason.Interrupted : SkillEndReason.Canceled);

        if (wasTargeting)
            CancelTargeting();

        Targeting.ClearTempTarget();

        if (!wasExecuting)
        {
            _hero.Move.SetCanMove(true);
            ClearData();
            CancelAnim();
            SafeInvoke(() => Canceled?.Invoke(), "Canceled");
            CmdBroadcastCanceled();
            SafeInvoke(() => OnSkillCanceled?.Invoke(), "OnSkillCanceled");
        }

        return true;
    }

    public void ResetSkillState()
    {
        if (_runActive)
            Interrupt(SkillEndReason.Interrupted);
        else if (_state != SkillState.Inactive)
            SetState(SkillState.Inactive, SkillEndReason.Interrupted);

        StopCoro(ref _preparingCoroutine);
        _preparingActive = false;
        PreparingEnded?.Invoke();

        _castTriggered = false;
        _cooldownCommitted = false;
        _isAutoMode = false;

        if (Charges.UsesCharges)
        {
            _currentChargers = Charges.MaxCharges;
            CurrentChargeChanged?.Invoke(_currentChargers);
        }

        StopCoro(ref _channelingCoroutine);
        _channelingActive = false;
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