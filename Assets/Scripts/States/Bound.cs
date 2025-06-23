using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bound : AbstractCharacterState
{
	public bool turnOff = false;
	private float _baseDuration;
	private float _duration;
	private static readonly int _stunTrigger = Animator.StringToHash("Rope");
	private static readonly int _stunTriggerExit = Animator.StringToHash("RopeExit");

	private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.Ability };
	public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
	public override States State => States.Bound;
	public override StateType Type => StateType.Physical;
	public override List<StatusEffect> Effects => _effects;


	public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		_characterState = character;

		if (character.TryGetComponent<Character>(out var ability))
		{
			_abilities = ability.Abilities;

			foreach (var skill in _abilities.Abilities) if (skill.Moving == Moving.NonStatic) skill.Disactive = true;
		}

		_characterState.Character.Move.IsMoveBlocked = true;
		_characterState.Character.Move.StopMoveAnimation();

		if(_characterState.TryGetComponent<StateEffects>(out StateEffects stateEffects)) stateEffects.RopeTrap.SetActive(true);

		_characterState.Character.Animator.SetTrigger(_stunTrigger);
		_characterState.Character.NetworkAnimator.SetTrigger(_stunTrigger);

		_duration = durationToExit;
		_baseDuration = durationToExit;
	}

	public override void UpdateState()
	{
		_duration -= Time.deltaTime;
		if (_duration < 0 || turnOff) ExitState();
	}

	public override void ExitState()
	{
		_characterState.RemoveState(this);
		if (!_characterState.Check(StatusEffect.Move)) _characterState.Character.Move.IsMoveBlocked = false;
		if (!_characterState.Check(StatusEffect.Ability) && _abilities != null) _abilities.SetAbilitiesEnabled();
		if (_characterState.TryGetComponent<StateEffects>(out StateEffects stateEffects)) stateEffects.RopeTrap.SetActive(false);
		_characterState.Character.Animator.ResetTrigger(_stunTrigger);
		_characterState.Character.NetworkAnimator.ResetTrigger(_stunTrigger);
		_characterState.Character.Animator.SetTrigger(_stunTriggerExit);
		_characterState.Character.NetworkAnimator.SetTrigger(_stunTriggerExit);

		if (!_characterState.Check(StatusEffect.Ability) && _abilities != null) foreach (var skill in _abilities.Abilities) skill.Disactive = false;
	}

	public override bool Stack(float time)
	{
		if (_baseDuration > time) return false;
		else
		{
			_duration = time;
			return true;
		}
	}
}

