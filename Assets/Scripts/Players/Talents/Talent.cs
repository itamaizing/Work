using System;
using System.Collections.Generic;
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
    

    public TalentSystem Owner { get; set; }
    
    private static readonly Dictionary<Type, FieldInfo[]> _skillFieldsCache = new();
    private bool? _effectOn;
    private int _effectLevel;

    public void Init()
    {
        _data.Init();
        _data.Name = GetType().Name;
        if (OpenCondition == null)
        {
            OpenCondition = new EmptyCondition();
        }
        _data.condition = OpenCondition;
        _data.ConditionDescription = OpenCondition.ConditionDescription();
    }

    public abstract void Enter();

    public abstract void Exit();
    
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
    
    public void SetActive(bool isActive, int lvl = 0)
    {
        _data.SetOpen(isActive);
        _data.SetLevel(lvl);
        ApplyEffect(isActive && OpenCondition.CanOpen);
    }


    public void AddDependendTalent(TalentData data)
    {
        if(data != null)
            _data.AddDependentTalent(data);
    }
}