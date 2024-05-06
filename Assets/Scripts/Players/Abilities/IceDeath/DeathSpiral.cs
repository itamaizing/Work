using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathSpiral : Ability
{
	protected override void Cancel()
	{
		//turn off targets and etc		
	}
	protected override void Cast()
	{
		PayCost();
	}
}
