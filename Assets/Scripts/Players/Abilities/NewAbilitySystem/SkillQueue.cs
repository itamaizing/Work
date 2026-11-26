using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SkillQueue : MonoBehaviour
{
    public interface IPreemptsQueue
    {
        /// <summary>
        /// Вызывается очередью, когда этот скилл — головной в очереди, а место занято другим (busySkill).
        /// Возвращает true, если скилл сам оборвал busySkill (через TryCancel) и заберёт его состояние.
        /// Если false — очередь продолжает ждать естественного завершения busySkill, как раньше.
        /// </summary>
        bool TryPreemptCurrentCast(Skill busySkill);
    }
    
    [SerializeField] private SkillRenderer _skillRenderer;

    private Queue<Skill> _skills = new Queue<Skill>();
    private Skill _currentSkill = null;
    private TargetInfo _targetInfo;
    private bool _waitingForApproval;
    private float _retryCastAt;
    private const float CastRetryDelay = 0.1f;

    public bool IsBusy { get => _currentSkill != null; }
    public bool IsEmpty { get => _skills.Count == 0; }
    public Skill CurrentSkill { get => _currentSkill; }
    public TargetInfo CurrentCastTarget { get; private set; }
    public Queue<Skill> Skills => _skills;

    public event Action<Skill> SkillAdded;
    public event Action<Skill> SkillDeleted;
    public event Action CurrentCastChanged;
    public event Action<Skill> Cancell;

    private void Update()
    {
        if (_skills.TryPeek(out Skill headSkill))
        {
            if (_waitingForApproval && headSkill == _currentSkill)
                headSkill = _skills.Skip(1).FirstOrDefault();
            if (IsBusy && headSkill is IPreemptsQueue preemptor)
            {
                preemptor.TryPreemptCurrentCast(_currentSkill);
            }
        }
        
        if (IsBusy)
            return;
        
        while (_skills.TryPeek(out var queuedSkill) && queuedSkill.TargetInfoQueue.TryPeek(out var queuedTarget) && queuedTarget.LostSelectedTarget)
        {
            if (queuedTarget.SelectedTarget != null && queuedTarget.SelectedTarget.SelectedCircle != null)
                queuedTarget.SelectedTarget.SelectedCircle.SwitchSelectCircle(false);

            queuedSkill.TargetInfoQueue.Dequeue();
            RemoveFromQueue();
        }

        if (_skills.TryPeek(out Skill skill))
        {
            if (Time.unscaledTime < _retryCastAt) return;
            if (skill.Targeting.SkillType == TargetingType.Zone)
                Draw(skill);

            if (skill.TargetInfoQueue.TryPeek(out TargetInfo targetInfo))
            {
                _targetInfo = targetInfo;
                foreach (var item in _targetInfo.GetTargets())
                {
                    if (item is Character character)
                    {
                        character.SelectedCircle.SwitchSelectCircle(true);
                    }
                }
            }

            if (targetInfo?.UsesSelectedTarget == true && !targetInfo.SelectedTarget.IsTargetable)
                return;

            if (skill.TryCast())
            {
                if (!skill.IsExecuting) return;

                _currentSkill = skill;
                _waitingForApproval = true;
                CurrentCastTarget = targetInfo;
                _currentSkill.CastStarted += OnCastStarted;
                _currentSkill.CastEnded += OnCastEnded;
                _currentSkill.CastRejected += OnCastRejected;
                CurrentCastChanged?.Invoke();
            }
        }
    }
    public void Add(Skill skill)
    {
        //if (_skills.Contains(skill))
        //return; 
        if (skill is IPassiveSkill) return;

        _skills.Enqueue(skill);
        SkillAdded?.Invoke(skill);
    }

    public bool TryCancel(bool isForceCancel = false)
    {
        return TryCancel(_currentSkill, isForceCancel);
    }
    
    public bool TryCancel(Skill skillForCancelling,bool isForceCancel = false)
    {
        if (skillForCancelling != null)
        {
            var target = skillForCancelling == _currentSkill ? CurrentCastTarget : null;
            bool canceled = skillForCancelling.TryCancel(isForceCancel);
            if (canceled) ToggleSelectCircles(target, false);
            return canceled;
        }

        var queuedSkill = RemoveFromQueue();

        if (queuedSkill != null)
        {
            if (queuedSkill.TargetInfoQueue.Count > 0)
            {
                var temp = queuedSkill.TargetInfoQueue.Dequeue();

                foreach (var item in temp.GetTargets())
                {
                    if (item is Character character)
                    {
                        character.SelectedCircle.SwitchSelectCircle(false);
                    }
                }

                queuedSkill.TryCancel(isForceCancel);
                Cancell?.Invoke(queuedSkill);
                return true;
            }
        }

        return false;
    }
    private void Draw(Skill skill)
    {
        if (_skillRenderer == null) return;
        if (skill.TargetInfoQueue == null || skill.TargetInfoQueue.Count == 0) return;

        var info = skill.TargetInfoQueue.Peek().Points;
        if (info == null || info.Count == 0) return;

        Vector3[] vector3s = new Vector3[info.Count];
        for (int i = 0; i < info.Count; i++) vector3s[i] = new Vector3(info[i].x, info[i].y + 0.1f, info[i].z);

        _skillRenderer.StartDrawAllLineForZone(vector3s);
        _skillRenderer.DrawRadius(skill.AreaInfo.Radius);
    }

    private Skill RemoveFromQueue()
    {
        if (_skills.Count == 0) return null;

        var temp = _skills.Dequeue();
        SkillDeleted?.Invoke(temp);

        if (temp.Targeting.SkillType == TargetingType.Zone)
        {
            _skillRenderer.StopDrawRadius();
            _skillRenderer.StopDrawAllLineForZone();
        }

        return temp;
    }

    public void RemoveNeededSkillFromQueue(Skill neededSkill)
    {
        if (_currentSkill != null && ShouldRemoveSkill(_currentSkill, neededSkill))
            TryCancel(_currentSkill, true);
        if (_skills.Count == 0) return;

        Queue<Skill> tempQueue = new Queue<Skill>();
        
        while (_skills.Count > 0)
        {
            Skill skill = _skills.Dequeue();
            
            if (ShouldRemoveSkill(skill, neededSkill))
            {
                SkillDeleted?.Invoke(skill);

                skill.ClearQueueTarget();
            }
            else
            {
                tempQueue.Enqueue(skill);
            }
        }
        
        _skills = tempQueue;
    }
    
    private bool ShouldRemoveSkill(Skill skill, Skill neededSkill)
    {
        if (skill.GetType() == neededSkill.GetType())
            return true;

        return false;
    }


    private void OnCastStarted()
    {
        if (!_waitingForApproval || _currentSkill == null) return;
        _waitingForApproval = false;
        RemoveFromQueue();
    }

    private void OnCastRejected()
    {
        if (_currentSkill == null) return;
        bool retry = _currentSkill.LastCastRequestCanRetry;
        if (_waitingForApproval && !retry) RemoveFromQueue();
        _waitingForApproval = false;
        _retryCastAt = retry ? Time.unscaledTime + CastRetryDelay : 0f;
        OnCastEnded();
    }

    private void OnCastEnded()
    {
        if (_currentSkill == null) return;
        if (_waitingForApproval)
        {
            _waitingForApproval = false;
            RemoveFromQueue();
        }
        ToggleSelectCircles(CurrentCastTarget, false);
        _currentSkill.CastStarted -= OnCastStarted;
        _currentSkill.CastEnded -= OnCastEnded;
        _currentSkill.CastRejected -= OnCastRejected;
        _currentSkill = null;
        _waitingForApproval = false;
        CurrentCastTarget = null;
        CurrentCastChanged?.Invoke();
    }

    private static void ToggleSelectCircles(TargetInfo info, bool isOn)
    {
        if (info?.GetTargets() == null) return;
        foreach (var target in info.GetTargets()) if (target is Character character && character?.SelectedCircle != null) character.SelectedCircle.SwitchSelectCircle(isOn);
    }
}
