using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ChannelComponent : BaseSkillComponent
{
    #region InspectorFields
    [SerializeField] protected float _castDuration;
    [SerializeField] protected float _tickInterval;
    [SerializeField] protected List<SkillEnergyCost> _baseCostPerTick = new();
    #endregion

    #region RuntimeVariables

    #endregion

    #region Properties
    public float CastDuration {
        get { return _castDuration; }
        set { _castDuration = value; }
    }
    public float TickInterval => _tickInterval;
    public List<SkillEnergyCost> Costs => _baseCostPerTick;

    #endregion

    #region Methods

    #endregion
}
