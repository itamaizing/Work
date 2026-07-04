using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Curse : AbstractCharacterState
{
	private Character _personWhoShooted;
	private float _durationToExit = 0;

	public override States State => States.Curse;
	public override StateType Type => StateType.Magic;
	public override List<StatusEffect> Effects => throw new System.NotImplementedException();
	public override BaffDebaff BaffDebaff => BaffDebaff.Baff;

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		characterState = character;
		_durationToExit = durationToExit;
		//if(character.personWhoShoted != null)
		//_personWhoShooted = character.personWhoShoted;
	}

	public override void UpdateState()
	{
		_durationToExit -= Time.deltaTime;
		if (_durationToExit < 0)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		characterState.RemoveState(this);
	}

	public override bool Stack(float time)
	{
		/*if (characterState.personWhoShoted != null)
		{
			_personWhoShooted = characterState.personWhoShoted;
		}*/
		return true;
	}
}
