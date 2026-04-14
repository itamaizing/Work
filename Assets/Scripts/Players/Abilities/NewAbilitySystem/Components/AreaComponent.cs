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
        get { return _skillAttributes[SkillAttributeName.Radius].GetValue(); }
        set { _skillAttributes[SkillAttributeName.Radius].SetBaseValue(value); }
    }
    public float Area
    {
        get { return _skillAttributes[SkillAttributeName.Area].GetValue(); }
        set { _skillAttributes[SkillAttributeName.Area].SetBaseValue(value); }
    }
    public float CastLength
    {
        get { return _skillAttributes[SkillAttributeName.Length].GetValue(); }
        set { _skillAttributes[SkillAttributeName.Length].SetBaseValue(value); }
    }
    public float CastWidth
    {
        get { return _skillAttributes[SkillAttributeName.Width].GetValue(); }
        set { _skillAttributes[SkillAttributeName.Width].SetBaseValue(value); }
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
