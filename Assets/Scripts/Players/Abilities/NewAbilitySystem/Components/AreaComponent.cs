using System;
using UnityEngine;

[Serializable]
public class AreaComponent : BaseSkillComponent
{
    #region InspectorFields
    [SerializeField] protected float _radius;
    [SerializeField] protected float _area;
    [SerializeField] protected float _castLength;
    [SerializeField] protected float _castWidth;
    #endregion

    #region RuntimeVariables

    #endregion

    #region Properties
    public float Radius
    {
        get
        {
            float baseValue = _skillAttributes.Attributes[SkillAttributeName.Radius].GetValue();
            return _skill.Buff.Radius.GetBuffedValue(baseValue);
        }

        set { _skillAttributes.Attributes[SkillAttributeName.Radius].SetBaseValue(value); }
    }
    public float Area
    {
        get
        {
            float baseValue = _skillAttributes.Attributes[SkillAttributeName.Area].GetValue();
            return _skill.Buff.Area.GetBuffedValue(baseValue);
        }

        set { _skillAttributes.Attributes[SkillAttributeName.Area].SetBaseValue(value); }
    }
    public float CastLength
    {
        get
        {
            float baseValue = _skillAttributes.Attributes[SkillAttributeName.Length].GetValue();
            return _skill.Buff.Length.GetBuffedValue(baseValue);
        }

        set { _skillAttributes.Attributes[SkillAttributeName.Length].SetBaseValue(value); }
    }
    public float CastWidth
    {
        get
        {
            float baseValue = _skillAttributes.Attributes[SkillAttributeName.Width].GetValue();
            return _skill.Buff.Width.GetBuffedValue(baseValue);
        }

        set { _skillAttributes.Attributes[SkillAttributeName.Width].SetBaseValue(value); }
    }
    #endregion

    #region Methods
    public override void Init(Skill skill)
    {
        base.Init(skill);
        Radius = _radius;
        Area = _area;
        CastLength = _castLength;
        CastWidth = _castWidth;
    }
    #endregion
}
