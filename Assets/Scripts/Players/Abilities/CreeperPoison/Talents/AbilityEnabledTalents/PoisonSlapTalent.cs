using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonSlapTalent : Talent
{
    [SerializeField] private PoisonSlap _poisonSlap;
    [SerializeField] private SkillManager _ability;

    public override void Enter()
    {
        if (_ability.Abilities.Contains(_poisonSlap))
        {
            _poisonSlap.enabled = true;
        }
        else
        {
            //_ability.AddAbility(_poisonSlap);
        }
    }

    public override void Exit()
    {
        if (_ability.Abilities.Contains(_poisonSlap))
        {
            //_ability.RemoveAbility(_poisonSlap);
            _poisonSlap.enabled = false;
        }
        else
        {
            _poisonSlap.enabled = false;
        }
    }
}
