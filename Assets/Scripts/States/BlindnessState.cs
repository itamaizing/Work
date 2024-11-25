using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlindnessState : AbstractCharacterState
{
	private float _duration;
	private float _baseDuration;

	private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Ability };

    public override float TEST_ChangeableValue { get; set; }
    public override States State => States.Blind;
	public override StateType Type => StateType.Physical;
    public override BuffDebuff BuffDebuff => BuffDebuff.Debuff;
    public override List<StatusEffect> Effects => _effects;

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		//Debug.Log("Entering Stunned State");
		_duration = durationToExit;
		_baseDuration = durationToExit;
		_characterState = character;
	}

	public override void UpdateState()
	{
		//Debug.Log("Updating Stunned State");
		_duration -= Time.deltaTime;
		if (_duration < 0)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		//Debug.Log("Exiting Stunned State");
		_characterState.RemoveState(this);
	}

	public override bool Stack(float time)
	{
		_duration = _baseDuration;
		return true;
	}
}
