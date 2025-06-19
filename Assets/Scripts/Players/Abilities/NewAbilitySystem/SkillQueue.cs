using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillQueue : MonoBehaviour
{
    [SerializeField] private SkillRenderer _skillRenderer;

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
            if (!skill.Disactive && skill.TryCast())
            {
                RemoveFromQueue();
                _currentSkill = skill;
                _currentSkill.CastEnded += OnCastEnded;
            }
        }
    }

    public void Add(Skill skill)
    {
        //if (_skills.Contains(skill))
            //return; 

        _skills.Enqueue(skill);
        SkillAdded?.Invoke(skill);

        var info = skill.TargetInfoQueue.Peek().Points;

        Vector3[] vector3s = new Vector3[info.Count];

        for (int i = 0; i < info.Count; i++)
            vector3s[i] = new Vector3(info[i].x, info[i].y + 0.1f, info[i].z);

        Debug.Log(_skillRenderer);
        _skillRenderer.StartDrawAllLineForZone(vector3s); 
    }

    public bool TryCancel(bool isFoceCancel = false)
    {
        if (_currentSkill != null)
        {
            _currentSkill.TryCancel(isFoceCancel);
            return true;
        }
        else if(IsEmpty == false)
        {
            RemoveFromQueue().TargetInfoQueue.Dequeue();
            return true;
        }
        return false;
    }

    private Skill RemoveFromQueue()
    {
        var temp = _skills.Dequeue();
        SkillDeleted?.Invoke(temp);
        _skillRenderer.StopDrawAllLineForZone();
        return temp;
    }

    private void OnCastEnded()
    {
        _currentSkill.CastEnded -= OnCastEnded;
        _currentSkill = null;
    }
}
