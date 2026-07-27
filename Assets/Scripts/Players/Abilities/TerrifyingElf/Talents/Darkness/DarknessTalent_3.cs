using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarknessTalent_3 : Talent
{
    [SerializeField] private Silence silence;
    [SerializeField] private TerrifyingElfAura terrifyingElfAura;

    public override void Enter()
    {
        character.Abilities.ActivateSkill(character.Abilities.GetSkill<RetributiveReckoning>());
    }

    public override void Exit()
    {
        character.Abilities.DeactivateSkill(character.Abilities.GetSkill<RetributiveReckoning>());
    }
}
