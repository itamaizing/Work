using UnityEngine;

public class NinjaPoisonTalent_8 : Talent
{
    [SerializeField] private SpitPoison _spitPoison;
    [SerializeField] private PoisonBall _poison;

    public override void Enter()
    {
        character.Abilities.GetSkill<CreeperInvisible>().TransparentPoisons(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<CreeperInvisible>().TransparentPoisons(false);
    }
}