using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrostTalent_9 : Talent
{
	public override void Enter()
	{
		character.Abilities.GetSkill<NinjaResources>().RepeatedFrost(true);
	}

	public override void Exit()
	{
		character.Abilities.GetSkill<NinjaResources>().RepeatedFrost(false);
	}
}
