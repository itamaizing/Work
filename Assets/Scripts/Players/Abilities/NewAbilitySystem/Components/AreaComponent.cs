using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AreaComponent : BaseSkillComponent
{
    #region Fields
    [SerializeField] protected float _radius;
    [SerializeField] protected float _area;
    [SerializeField] protected float _castLength;
    [SerializeField] protected float _castWidth;
    #endregion

    #region Properties
    public float Radius
    {
        get
        {
            float final = _skillBuffs.Radius.GetBuffedValue(_radius);
            return final;
        }
        set { _radius = value; }
    }
    public float Area
    {
        get
        {
            float final = _skillBuffs.Area.GetBuffedValue(_area);
            return final;
        }
        set { _area = value; }
    }
    public float CastLength
    {
        get
        {
            float final = _skillBuffs.Length.GetBuffedValue(_castLength);
            return final;
        }
        set { _castLength = value; }
    }
    public float CastWidth
    {
        get
        {
            float final = _skillBuffs.Width.GetBuffedValue(_castWidth);
            return final;
        }
        set { _castWidth = value; }
    }
    #endregion
}
