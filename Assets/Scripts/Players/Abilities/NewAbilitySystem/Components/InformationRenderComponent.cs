using System;
using TMPro;
using UnityEngine;

[Serializable]
public class InformationRenderComponent : BaseSkillComponent
{
    #region InspectorFields
    [SerializeField] protected bool _isAutoRadiusRender = true;
    [SerializeField] protected bool _isAutoAreaRender = true;
    [SerializeField] protected bool _isAutoLineRender = true;
    [SerializeField] protected bool _isDynamicRenderer = false;
    #endregion
    
    #region RuntimeVariables

    #endregion

    #region Properties
    public bool IsAutoRadiusRender => _isAutoRadiusRender;
    public bool IsAutoAreaRender => _isAutoAreaRender;
    public bool IsAutoLineRender => _isAutoLineRender;
    public bool IsDynamicRenderer => _isDynamicRenderer;
    #endregion

    #region Methods
    public void ShowAOEIndicator(Vector3 position, bool isCommand=false)
    {
        Damage damage = new Damage
        {
            Value = _skill.Damage,
            Type = _skill.Info.DamageType,
        };

        if (isCommand)
        {
            _skill.SkillRender.CmdDrawDamageZone(position, _skill.AreaInfo.Area, damage, _character.gameObject);
        }
        else
        {
            _skill.SkillRender.DrawDamageZone(position, _skill.AreaInfo.Area, damage, _character.gameObject);
        }
    }

    public void HideAOEIndicator(bool isCommand=false)
    {
        if (isCommand)
        {
            _skill.SkillRender.CmdRemoveNextDamageZone();
        }
        else
        {
            _skill.SkillRender.RemoveNextDamageZone();
        }
    }

    public void ShowSmartIndicator()
    {
        Damage damage = new Damage
        {
            Value = _skill.Damage,
            Type = _skill.Info.DamageType,
        };

        if (_isAutoRadiusRender)
            _skill.SkillRender.DrawRadius(_skill.AreaInfo.Radius);

        if (_isAutoAreaRender)
        {
            _skill.SkillRender.DrawArea(_skill.AreaInfo.Area, damage, _skill.Targeting.Layer);
            _skill.SkillRender.StartDynamicRadiusColor(_skill.AreaInfo.Radius, _skill);
        }

        _skill.SkillRender.StartPreview(_skill.AreaInfo.Area, damage, _skill.Targeting.Layer);

        if (_isAutoLineRender)
        {
            Debug.Log("Auto line " + _skill, _skill);
            _skill.SkillRender.DrawLine(_skill.AreaInfo.CastLength, _skill.AreaInfo.CastWidth, damage, _skill.Targeting.Layer);
        }

        switch (_skill.Targeting.SkillType)
        {
            case SkillType.Target:
                _skill.SkillRender.DrawClosestTarget(_skill.AreaInfo.Radius, _skill.Targeting.Layer, _character);
                break;
            case SkillType.Zone:
                _skill.SkillRender.StartDrawLineForZone(_skill);
                break;
        }
    }

    public void HideSmartIndicator()
    {
        _skill.SkillRender.ResetCursor();
        _skill.SkillRender.StopDrawRadius();
        _skill.SkillRender.StopDrawArea();
        _skill.SkillRender.StopDrawLine();
        _skill.SkillRender.StopDrawClosestTarget();
        _skill.SkillRender.StopDynamicRadiusColor();

        _skill.SkillRender.StopPreview();
        _skill.StopDynamicRender();

        if (_skill.Targeting.SkillType == SkillType.Zone)
        {
            _skill.SkillRender.StopDrawLineForZone();
        }


        /*if (true)
		{
			Character enemy = GetCloserTargets(transform.position, AreaInfo.Radius)[0];
			enemy.SelectedCircle.IsActive = false;
		}*/
    }

    #endregion Methods
}
