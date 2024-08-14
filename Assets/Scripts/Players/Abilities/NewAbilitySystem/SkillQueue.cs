using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillQueue : MonoBehaviour
{
    private Queue<Skill> _skills = new Queue<Skill>();
    private Skill _currentSkill = null;

    public bool IsBusy { get => _currentSkill != null; }
    public bool IsEmpty { get => _skills.Count > 0; }
    public Skill CurrentSkill { get => _currentSkill; }

    public event Action<Skill> SkillAdded;
    public event Action<Skill> SkillDeleted;

    private void Update()
    {
        if (IsBusy)
            return;

        if(_skills.TryPeek(out Skill skill))
        {
            if (skill.TryCast())
            {
                RemoveFromQueue();
                _currentSkill = skill;
                _currentSkill.CastEnded += OnCastEnded;
            }
        }
    }

    public void Add(Skill skill)
    {
        _skills.Enqueue(skill);
    }

    public bool TryCancel(bool foceCancel = false)
    {
        if (_currentSkill != null)
        {
            if (_currentSkill.TryCancel(foceCancel))
                return true;
        }
        else
        {
            RemoveFromQueue();
            return true;
        }
        return false;
    }

    private void RemoveFromQueue()
    {
        SkillDeleted?.Invoke(_skills.Dequeue());
    }

    private void OnCastEnded()
    {
        _currentSkill.CastEnded -= OnCastEnded;
        _currentSkill = null;
    }
}
