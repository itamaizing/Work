using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudExplosionTalent : Talent
{
    [SerializeField] private CloudExplosion _cloudExplosion;
    [SerializeField] private PlayerAbilities _ability;
    public override void Enter()
    {
        if (_ability.Abilities.Contains(_cloudExplosion))
        {
            _cloudExplosion.enabled = true;
        }
        else
        {
            _ability.AddAbility(_cloudExplosion);
        }
    }

    public override void Exit()
    {
        if (_ability.Abilities.Contains(_cloudExplosion))
        {
            _ability.RemoveAbility(_cloudExplosion);
            _cloudExplosion.enabled = false;
        }
        else
        {
            _cloudExplosion.enabled = false;
        }
    }
}
