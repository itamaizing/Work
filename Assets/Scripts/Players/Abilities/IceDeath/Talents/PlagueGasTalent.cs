using UnityEngine;

public class PlagueGasTalent : Talent
{
	[SerializeField] DeathSpiral _deathSpiral;
	public override void Enter()
	{
		_deathSpiral.TalentCorpseBoostExplode(true);
	}

	public override void Exit()
	{
		_deathSpiral.TalentCorpseBoostExplode(false);
	}
}
