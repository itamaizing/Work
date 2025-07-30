using System;
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
            if (skill.SkillType == SkillType.Zone)
                Draw(skill);

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
    }

    public bool TryCancel(bool isForceCancel = false)
    {
        if (_currentSkill != null)
        {
            _currentSkill.TryCancel(isForceCancel);
            return true;
        }

        var queuedSkill = RemoveFromQueue();
        if (queuedSkill != null)
        {
            if (queuedSkill.TargetInfoQueue.Count > 0) queuedSkill.TargetInfoQueue.Dequeue();

            queuedSkill.TryCancel(isForceCancel);
            return true;
        }

        return false;
    }

    private void Draw(Skill skill)
    {
        if (skill.TargetInfoQueue.Count == 0) return;

        var info = skill.TargetInfoQueue.Peek().Points;

        Vector3[] vector3s = new Vector3[info.Count];
        for (int i = 0; i < info.Count; i++)
            vector3s[i] = new Vector3(info[i].x, info[i].y + 0.1f, info[i].z);

        _skillRenderer.StartDrawAllLineForZone(vector3s);
        _skillRenderer.DrawRadius(skill.Radius);
    }

    private Skill RemoveFromQueue()
    {
        if (_skills.Count == 0) return null;

        var temp = _skills.Dequeue();
        SkillDeleted?.Invoke(temp);

        if (temp.SkillType == SkillType.Zone)
        {
            _skillRenderer.StopDrawRadius();
            _skillRenderer.StopDrawAllLineForZone();
        }

        return temp;
    }


    private void OnCastEnded()
    {
        _currentSkill.CastEnded -= OnCastEnded;
        _currentSkill = null;
    }
}
