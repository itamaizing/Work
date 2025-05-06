using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrowTreeHealthTalent : Talent
{
	[SerializeField] private GrowTree growTree;

	public override void Enter()
	{
		growTree.treeHealthTalentActive(true);
	}

	public override void Exit()
	{
		growTree.treeHealthTalentActive(false);
	}
}
