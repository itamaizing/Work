using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrozenState : AbstractCharacterState
{
	//public bool turnOff = false;
	private float _duration;
	private float _baseDuration;
	private float _damageToExit;

	private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.Ability };
	public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
	public override States State => States.Frozen;
	public override StateType Type => StateType.Magic;
	public override List<StatusEffect> Effects => _effects;

    public override float TEST_ChangeableValue { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		Debug.Log("Entering Frozen State");
		_characterState = character;
		_duration = durationToExit;
		if (damageToExit == 0)
		{
			_damageToExit = 10000;
		}
		else
		{
			_damageToExit = damageToExit;
		}

		_characterState.Character.Move.CanMove = false;

		if (character.TryGetComponent<Character>(out var ability))
		{
			_abilities = ability.Abilities;
			_abilities.SetAbilitiesDisabled();
		}
		else
		{
			Debug.Log("no ability at " + character.gameObject.name);
		}

		//_characterState.Health.sumDamageTaken = 0;

	}

	public override void UpdateState()
	{
		_duration -= Time.deltaTime;
		if (_characterState.Character.Health.SumDamageTaken >= _damageToExit || _duration <= 0 )//|| turnOff)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		Debug.Log("Exiting Frozen State");

		//character.GetAbilityManager().ToggleAbility(true);//turn on abilities
		_characterState.RemoveState(this);
		if (_characterState.Check(StatusEffect.Move))
		{
			_characterState.Character.Move.CanMove = true;
		}
		if (_characterState.Check(StatusEffect.Ability) && _abilities != null)
		{
			_abilities.SetAbilitiesEnabled();
		}
	}

	public override bool Stack(float time)
	{
		_duration = _baseDuration;
		return true;
	}
}
