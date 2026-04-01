using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TagComponent : BaseSkillComponent
{
    #region InspectorFields
    public List<Enum> tags;
    public Tag_SkillType skillType;
    public Tag_EffectType effect;
    public Tag_Duration duration;
    public Tag_Mobility mobility;
    public Tag_Control control;
    [SerializeField] public List<Enum> Test;
    #endregion

    #region RuntimeVariables

    #endregion

    #region Properties
    public float Template
    {
        get { return 0; }
        set { value++; }
    }

    #endregion

    #region Methods
    public override void Init(Skill skill)
    {
        base.Init(skill);
        tags.Add(skillType);
        tags.Add(effect);
        tags.Add(duration);
        tags.Add(mobility);
        tags.Add(control);
    }

    public bool Has(Enum tag)
    {
        Type tagType = tag.GetType();
        foreach(var t in tags)
        {
            if (t.GetType() == tag.GetType())
                if (((int)(object)t & (int)(object)tag) != 0)
                    return true;
        }
        return false;
    }

    public bool Has(Type type, Enum tag)
    {
        return false;
    }
    #endregion
}

[Flags]
public enum Tag_SkillType
{
    None,
    Steroid,
    Debuff,
}

[Flags]
public enum Tag_EffectType
{
    None,
    Damage,
    Heal,
    Status,
}


[Flags]
public enum Tag_Duration
{
    None,
    Instant,
    OverTime,
}

[Flags]
public enum Tag_Mobility
{
    None,
    Dash,
    Teleport,
    Buff,
}


[Flags]
public enum Tag_Control // Soft/Hard vs Root/Stun/Disarm/Silence?
{
    None,
    Soft,
    Hard,
}
