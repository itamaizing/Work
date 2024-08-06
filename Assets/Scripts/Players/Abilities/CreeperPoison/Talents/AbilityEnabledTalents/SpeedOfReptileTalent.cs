using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeedOfReptileTalent : Talent
{
    [SerializeField] private SpeedOfReptile _speedOfReptile;
    [SerializeField] private PlayerAbilities _ability;

    public override void Enter()
    {
        if (_ability.Abilities.Contains(_speedOfReptile))
        {
            _speedOfReptile.enabled = true;
        }
        else
        {
            _ability.AddAbility(_speedOfReptile);
        }
    }

    public override void Exit()
    {
        if (_ability.Abilities.Contains(_speedOfReptile))
        {
            _ability.RemoveAbility(_speedOfReptile);
            _speedOfReptile.enabled = false;
        }
        else
        {
            _speedOfReptile.enabled = false;
        }
    }
}
