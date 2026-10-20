using UnityEngine;

public class LightTalent_8 : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<Restoration>()?.RestorationManaBooster?.Enable(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<Restoration>()?.RestorationManaBooster?.Enable(false);
    }
}
