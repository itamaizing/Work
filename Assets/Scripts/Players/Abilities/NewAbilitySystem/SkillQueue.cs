using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillQueue : MonoBehaviour
{
    private Queue<Skill> _skills = new Queue<Skill>();
    private Skill _currentSkill = null;

    public bool IsBusy { get => _currentSkill != null; }
    public bool IsEmpty { get => _skills.Count == 0; }
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
        if (_skills.Contains(skill))
            return; 

        _skills.Enqueue(skill);
        SkillAdded?.Invoke(skill);
    }

    public bool TryCancel(bool foceCancel = false)
    {
        if (_currentSkill != null)
        {
            _currentSkill.TryCancel(foceCancel);
            return true;
        }
        else if(IsEmpty == false)
        {
            RemoveFromQueue();
            return true;
        }
        return false;
    }

    private void RemoveFromQueue()
    {
        var temp = _skills.Dequeue();
        SkillDeleted?.Invoke(temp);
    }

    private void OnCastEnded()
    {
        _currentSkill.CastEnded -= OnCastEnded;
        _currentSkill = null;
    }
}
