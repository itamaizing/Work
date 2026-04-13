using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NatureTalent_14 : Talent
{
	[SerializeField] private ShotIntoSky shotIntoSky;
	[SerializeField] private ShotsIntoSky shotsIntoSky;
	[SerializeField] private PullingHealth pullingHealth;

	public override void Enter()
	{
		shotIntoSky.SetTripleShotTalentActive(true);
		shotsIntoSky.SetTripleShotTalentActive(true);
		pullingHealth.PullingHealthSpeedWithFearTalentActive(true);
	}

	public override void Exit()
	{
		shotIntoSky.SetTripleShotTalentActive(false);
		shotsIntoSky.SetTripleShotTalentActive(false);
		pullingHealth.PullingHealthSpeedWithFearTalentActive(false);
	}
}
