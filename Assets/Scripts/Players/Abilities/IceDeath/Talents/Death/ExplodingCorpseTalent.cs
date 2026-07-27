using UnityEngine;

public class ExplodingCorpseTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<PortalDarkness>().EnableExplodingCorpse(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<PortalDarkness>().EnableExplodingCorpse(false);
    }
}
