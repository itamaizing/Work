using System;
using UnityEngine;

[Serializable]
public class InformationRenderComponent : BaseSkillComponent
{
    #region Fields
    [SerializeField] protected bool _isAutoRadiusRender = true;
    [SerializeField] protected bool _isAutoAreaRender = true;
    [SerializeField] protected bool _isAutoLineRender = true;
    [SerializeField] protected bool _isDynamicRenderer = false;
    #endregion

    #region Properties
    public bool IsAutoRadiusRender => _isAutoRadiusRender;
    public bool IsAutoAreaRender => _isAutoAreaRender;
    public bool IsAutoLineRender => _isAutoLineRender;
    public bool IsDynamicRenderer => _isDynamicRenderer;
    #endregion
}
