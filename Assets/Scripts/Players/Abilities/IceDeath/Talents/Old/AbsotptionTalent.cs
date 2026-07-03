using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbsotptionTalent : Talent
{
	public override void Enter()
	{
		character.Abilities.ActivateSkill(character.Abilities.GetSkill<IceDeathAbsorbation>());
	}

	public override void Exit()
	{
		character.Abilities.DeactivateSkill(character.Abilities.GetSkill<IceDeathAbsorbation>());
	}
}
