using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NatureTalent_14 : Talent
{
	[SerializeField] private ShotIntoSky shotIntoSky;
	[SerializeField] private ShotsIntoSky shotsIntoSky;
	[SerializeField] private SleepSpell sleep;
	[SerializeField] private SkillManager _ability;

	public override void Enter()
	{
		_ability.ActivateSkill(sleep);
		shotIntoSky.SetTripleShotTalentActive(true);
		shotsIntoSky.SetTripleShotTalentActive(true);
	}

	public override void Exit()
	{
		_ability.DeactivateSkill(sleep);
		shotIntoSky.SetTripleShotTalentActive(false);
		shotsIntoSky.SetTripleShotTalentActive(false);
	}
}
