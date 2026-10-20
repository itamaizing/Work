using UnityEngine;

public class DisciplineTalent_10 : Talent
{
	public override void Enter()
	{
		character.Abilities.ActivateSkill(character.Abilities.GetSkill<EmeraldSkin>());
	}

	public override void Exit()
	{
		character.Abilities.DeactivateSkill(character.Abilities.GetSkill<EmeraldSkin>());
	}
}

