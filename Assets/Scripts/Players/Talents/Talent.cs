using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public abstract class Talent : MonoBehaviour
{
    [SerializeField]
    private TalentData _data;

    [SerializeReference, SubclassSelector]
    public OpenCondition OpenCondition = new EmptyCondition();

    public Character character;

    public TalentData Data => _data;
    private TalentSystem _owner;
    private int _groupId;
    private int _rowIndex;

    public TalentSystem Owner { get; set; }
    
    private static readonly Dictionary<Type, FieldInfo[]> _skillFieldsCache = new();
    private bool? _effectOn;
    private int _effectLevel;

    public void Init(TalentSystem owner, int groupId, int rowIndex)
    {
        _owner = owner;
        _groupId = groupId;
        _rowIndex = rowIndex;

        _data.Init();
        _data.Name = GetType().Name;
        _data.Group = groupId;
        _data.Row = rowIndex;

        if (OpenCondition == null) OpenCondition = new EmptyCondition();
        _data.condition = OpenCondition;

        if (OpenCondition is IScopedCondition scoped)
            scoped.SetOwner(owner, groupId);

        _data.ConditionDescription = OpenCondition.ConditionDescription();
    }

    public abstract void Enter();

    public abstract void Exit();

    public void SetActive(bool isActive, int lvl = 0)
    {
	    _data.SetOpen(isActive);
	    _data.SetLevel(isActive ? lvl : 0);
	    ApplyEffect(isActive && OpenCondition.CanOpen);
    }

    public bool TrySetActive(bool isActive, int lvl = 0)
    {
	    if (isActive && !OpenCondition.CanOpen) return false;
	    SetActive(isActive, lvl);
	    return true;
    }

    public bool IsRowLocked()
    {
	    var group = _owner?.TalentsGroups.FirstOrDefault(g => g.ID == _groupId);
	    return group != null && !group.CanOpenRow(_owner, _rowIndex);
    }

    public string GetRowConditionDescriptionIfLocked()
    {
        if (_owner == null) return string.Empty;
        var group = _owner.TalentsGroups.FirstOrDefault(g => g.ID == _groupId);
        if (group == null) return string.Empty;
        if (group.CanOpenRow(_owner, _rowIndex)) return string.Empty;
        return group.GetRowConditionDescription(_rowIndex);
    }

    public string GetLockDescription()
    {
        var parts = new List<string>();

        if (!OpenCondition.CanOpen && !string.IsNullOrEmpty(Data.ConditionDescription))
            parts.Add(Data.ConditionDescription);

        string rowDescription = GetRowConditionDescriptionIfLocked();
        if (!string.IsNullOrEmpty(rowDescription))
            parts.Add(rowDescription);

        return string.Join("\n", parts);
    }
    
    private static FieldInfo[] GetSkillFields(Type type)
    {
        if (_skillFieldsCache.TryGetValue(type, out var cached)) return cached;

        var list = new List<FieldInfo>();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public |
                                   BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        for (var t = type; t != null && t != typeof(Talent); t = t.BaseType)
            foreach (var f in t.GetFields(flags))
                if (typeof(Skill).IsAssignableFrom(f.FieldType))
                    list.Add(f);

        var result = list.ToArray();
        _skillFieldsCache[type] = result;
        return result;
    }
    
    protected virtual IEnumerable<Skill> GetGrantedSkills()
    {
        foreach (var f in GetSkillFields(GetType()))
            if (f.GetValue(this) is Skill s && s.IsTalentSpell)
                yield return s;
    }
    
    private SkillManager GetPreviewSkillManager()
    {
        var hero = GetComponentInParent<Character>();
        return hero != null ? hero.Abilities : GetComponentInParent<SkillManager>();
    }
    
    private bool ApplyPreviewEffect(bool on)
    {
        if (_effectOn == on) return false;

        if (!on && _effectOn != true)
        {
            _effectOn = false;
            return false;
        }

        _effectOn = on;

        var manager = GetPreviewSkillManager();
        if (manager == null) return false;

        foreach (var skill in GetGrantedSkills())
        {
            if (on) manager.ActivateSkill(skill);
            else    manager.DeactivateSkill(skill);
        }
        return true;
    }
    
    public bool ApplyEffect(bool on)
    {
        if (Owner != null && Owner.IsPreview)
            return ApplyPreviewEffect(on);

        int level = _data.Level;
        if (_effectOn == on && (!on || _effectLevel == level)) return false;

        if (on && _effectOn == true)
            Exit();

        _effectOn = on;
        _effectLevel = level;

        if (on) Enter();
        else Exit();
        return true;
    }


    public void AddDependendTalent(TalentData data)
    {
        if(data != null)
            _data.AddDependentTalent(data);
    }
}