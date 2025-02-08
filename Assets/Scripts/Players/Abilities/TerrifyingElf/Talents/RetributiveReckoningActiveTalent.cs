using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RetributiveReckoningActiveTalent : Talent
{
    [SerializeField] private RetributiveReckoning retributiveReckoning;
    [SerializeField] private SkillManager _ability;

    public override void Enter()
    {
        _ability.ActivateSkill(retributiveReckoning);
    }

    public override void Exit()
    {
        _ability.DeactivateSkill(retributiveReckoning);
    }
}
