using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisciplineTalent_5 : Talent
{
	[SerializeField] private SoulAid _soulAid;
	[SerializeField] private SkillManager _ability;

	public override void Enter()
	{
		character.Abilities.GetSkill<PriestShield>()?.EnableBooster(PriestShield.PriestShieldBoosterType.PhysicalShieldBoost, true);
	}

	public override void Exit()
	{
		character.Abilities.GetSkill<PriestShield>()?.EnableBooster(PriestShield.PriestShieldBoosterType.PhysicalShieldBoost, false);
	}
}
