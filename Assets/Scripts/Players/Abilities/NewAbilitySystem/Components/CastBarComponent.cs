using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CastBarComponent : BaseSkillComponent
{
    #region InspectorFields
    [SerializeField] private bool _shouldShow;
    //Duration брать из анимации?
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
    }

    #endregion
}
