using System.Collections.Generic;
using UnityEngine;

public class InvisibleState : AbstractCharacterState
{
	public bool turnOff = false;
	private float _baseDuration;
	private float _duration;
	private List<StatusEffect> _effects = new List<StatusEffect>();

	public override States State => States.Invisible;
	public override StateType Type => StateType.Magic;
	public override List<StatusEffect> Effects => _effects;

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		Debug.Log("Entering Invisible State");
		//effects.Add(StatusEffect.Others);

		_characterState = character;
		//_characterState.Health.SetInvincible(true);
		_characterState.invincible = true;
		_duration = durationToExit;
		_baseDuration = durationToExit;
	}

	public override void UpdateState()
	{
		Debug.Log("Updating Invisible State");
		_duration -= Time.deltaTime;
		if (_duration < 0 || turnOff)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		Debug.Log("Exiting Invisible State");
		if (_characterState.Check(StatusEffect.Others))
		{
			//_characterState.Health.SetInvincible(false);
			_characterState.invincible = false;
		}
		_characterState.RemoveState(this);
	}

	public override bool Stack(float time)
	{
		if (_baseDuration > time)
		{
			return false;
		}
		else
		{
			_duration = time;
			return true;
		}
	}
}
