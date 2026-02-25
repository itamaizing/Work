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
    public float Radius {
        get { return _skillAttributes.Attributes[SkillAttributeName.Radius].GetValue(); }
        set { _skillAttributes.Attributes[SkillAttributeName.Radius].SetBaseValue(value); }
    }
    public float Area {
        get { return _skillAttributes.Attributes[SkillAttributeName.Area].GetValue(); }
        set { _skillAttributes.Attributes[SkillAttributeName.Area].SetBaseValue(value); }
    }
    public float CastLength {
        get { return _skillAttributes.Attributes[SkillAttributeName.Length].GetValue(); }
        set { _skillAttributes.Attributes[SkillAttributeName.Length].SetBaseValue(value); }
    }
    public float CastWidth {
        get { return _skillAttributes.Attributes[SkillAttributeName.Width].GetValue(); }
        set { _skillAttributes.Attributes[SkillAttributeName.Width].SetBaseValue(value); }
    }
    #endregion
}
