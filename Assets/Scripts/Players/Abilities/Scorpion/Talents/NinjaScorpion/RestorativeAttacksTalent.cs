using UnityEngine;

public class RestorativeAttacksTalent : Talent
{
    [SerializeField] private RestorativeAttacks_Scorpion _skill;

    public override void Enter()
    {
        Hero.Abilities.ActivateSkill(_skill);
    }

    public override void Exit()
    {
        Hero.Abilities.DeactivateSkill(_skill);
    }

    private HeroComponent Hero => character.GetComponent<HeroComponent>();
}