using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoSkillQueue : MonoBehaviour
{
    private List<Skill> _skills = new();
    private Skill _currentSkill = null;
    private bool _waitingForApproval;

    public bool IsBusy { get => _currentSkill != null; }
    public bool IsEmpty { get => _skills.Count == 0; }
    public Skill CurrentSkill { get => _currentSkill; }

    public event Action<Skill> SkillAdded;
    public event Action<Skill> SkillDeleted;
    public event Action<Skill> SkillActivated;

    private void Update()
    {
        if (IsBusy || IsEmpty)
            return;

        TryUseSkill();
    }

    public void Add(Skill skill)
    {
        if (_skills.Contains(skill))
            return;

        _skills.Add(skill);
        SkillAdded?.Invoke(skill);
    }

    public bool TryCancel(bool foceCancel = false)
    {
        if (_currentSkill != null)
        {
            _currentSkill.TryCancel(foceCancel);

            return true;
        }
        else if (IsEmpty == false)
        {
            foreach (var item in _skills)
            {
                SkillDeleted?.Invoke(item);
            }
            _skills.Clear();

            return true;
        }
        return false;
    }

    private bool TryUseSkill()
    {
        foreach (var item in _skills)
        {
            if (item.TryCast())
            {
                _currentSkill = item;
                _waitingForApproval = true;
                _currentSkill.CastStarted += OnCastStarted;
                _currentSkill.CastEnded += OnCastEnded;
                _currentSkill.CastRejected += OnCastRejected;

                return true;
            }
        }
        return false;
    }

    private void OnCastStarted()
    {
        if (!_waitingForApproval) return;
        _waitingForApproval = false;
        foreach (var skill in _skills) SkillDeleted?.Invoke(skill);
        _skills.Clear();
        SkillActivated?.Invoke(_currentSkill);
    }

    private void OnCastRejected()
    {
        if (!_currentSkill.LastCastRequestCanRetry && _skills.Remove(_currentSkill))
            SkillDeleted?.Invoke(_currentSkill);
        _waitingForApproval = false;
        OnCastEnded();
    }

    private void OnCastEnded()
    {
        if (_waitingForApproval) OnCastStarted();
        _currentSkill.CastStarted -= OnCastStarted;
        _currentSkill.CastEnded -= OnCastEnded;
        _currentSkill.CastRejected -= OnCastRejected;
        _currentSkill = null;
    }
}
