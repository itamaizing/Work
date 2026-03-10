using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClapOfLightUpTalent : Talent
{
    [SerializeField] private Gangdollarff.ClapOfLight _skill;
    public override void Enter()
    {
        _skill.AreaInfo.Radius = _skill.AreaInfo.Radius * 1.5f;
    }

    public override void Exit()
    {
        _skill.AreaInfo.Radius = _skill.AreaInfo.Radius / 1.5f;
    }
}
