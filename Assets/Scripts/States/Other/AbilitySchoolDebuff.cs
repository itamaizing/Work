using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilitySchoolDebuff : AbstractCharacterState
{
	public bool turnOff = false;
	private float _baseDuration;
	public Schools canceledSchoool;
	
	private Character _character;

	private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.AbilitySchool };
	public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
	public override States State => States.SchoolDebuff;
	public override StateType Type => StateType.Immaterial;
	public override List<StatusEffect> Effects => _effects;



    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		characterState = character;
		
		_character = character.GetComponent<Character>();
		
		var abilities = _character.Abilities;
		
		Debug.LogError(abilities.CurrentCastingSkill);
		
		if (abilities.CurrentCastingSkill != null)
		{
			Debug.LogError("Casting not null");
			abilities.CurrentCastingSkill.CmdCancelActiveSkill();
		}

		if (character.TryGetComponent<Character>(out var ability))
		{
			abilities = ability.Abilities;
			abilities.SwitchAvaliable(canceledSchoool, false);
		}
		else
		{
			Debug.Log("no ability at " + character.gameObject.name);
		}
		_baseDuration = durationToExit;
	}

	public override void UpdateState()
	{
		if (turnOff)
		{
			GlobalExit();
		}
	}

	protected override void ExitState()
	{
		if (!characterState.Check(StatusEffect.Ability) && abilities != null)
		{
			abilities.SwitchAvaliable(canceledSchoool, true);
		}
	}

	/*public override bool Stack(float time)
	{
		duration = time;
		return true;
	}*/
}
