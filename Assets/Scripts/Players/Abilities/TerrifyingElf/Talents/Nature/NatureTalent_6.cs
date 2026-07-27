using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NatureTalent_6 : Talent
{
	//[SerializeField] private GrowTree growTree;

	public override void Enter()
	{
		character.Abilities.GetSkill<SleepSpell>().SleepInnerDarknessTalent(true);
		//growTree.treeHealthTalentActive(true);
	}

	public override void Exit()
	{
		character.Abilities.GetSkill<SleepSpell>().SleepInnerDarknessTalent(false);
		//growTree.treeHealthTalentActive(false);
	}
}
