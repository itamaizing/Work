using UnityEngine;

public class RestorationManaTalent : Talent
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
