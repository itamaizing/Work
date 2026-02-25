using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CostComponent : BaseSkillComponent
{
    #region InspectorFields
    [SerializeField] protected float _base;
    [SerializeField] protected List<float> _additional;
    #endregion

    #region Runtime Variables

    #endregion

    #region Properties
    public float Template {
        get {
            return 0;
        }
        set { }
    }
    #endregion

    #region Methods

    public bool EnoughResources()
    {
        return false;
    }

    public bool Pay()
    {

        return false;
    }

    #endregion
}
