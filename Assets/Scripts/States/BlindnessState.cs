using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlindnessState : AbstractCharacterState
{
	public bool turnOff = false;

	//private CharacterState _characterState;
	private float _duration;
	private float _baseDuration;

	private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Ability };
	public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
	public override States State => States.Blind;
	public override StateType Type => StateType.Physical;
	public override List<StatusEffect> Effects => _effects;


    //private PlayerAbilities _abilities;
    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		Debug.Log("Entering Stunned State");
		_duration = durationToExit;
		_baseDuration = durationToExit;
		_characterState = character;
		if (character.TryGetComponent<Character>(out var ability))
		{
			_abilities = ability.Abilities;
			_abilities.SetAbilitiesDisabled();
		}
		else
		{
			Debug.Log("no ability at " + character.gameObject.name);
		}
	}

	public override void UpdateState()
	{
		Debug.Log("Updating Stunned State");
		_duration -= Time.deltaTime;
		if (_duration < 0 || turnOff)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		Debug.Log("Exiting Stunned State");
		_characterState.RemoveState(this);
		if (!_characterState.Check(StatusEffect.Ability))
		{
			_abilities.SetAbilitiesEnabled();
		}
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
