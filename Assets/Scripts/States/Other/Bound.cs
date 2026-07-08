using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Bound : AbstractCharacterState
{
	public bool turnOff = false;
	private float _baseDuration;
	private float _duration;
	private static readonly int _stunTrigger = Animator.StringToHash("Rope");
	private static readonly int _stunTriggerExit = Animator.StringToHash("RopeExit");
	private GameObject _spawnedTrap;
	private bool _stateClosing;

	private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.Ability };
	public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
	public override States State => States.Bound;
	public override StateType Type => StateType.Physical;
	public override List<StatusEffect> Effects => _effects;

	private IDamageable _trapDamageable;

	public void SetTrapObject(GameObject trap)
	{
		_spawnedTrap = trap;
	}

	public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		characterState = character;
		_stateClosing = false;
		turnOff = false;
		_spawnedTrap = null;

		if (character.TryGetComponent<Character>(out var ability))
		{
			abilities = ability.Abilities;

			foreach (var skill in abilities.Abilities) if (skill.Info.Moving == Moving.NonStatic) skill.Disactive = true;
		}

		characterState.Character.Move.IsMoveBlocked = true;
		characterState.Character.Move.StopMoveAndAnimationMove();

		var animation = characterState.Character.Animator;
		var networkAnimation = characterState.Character.NetworkAnimator;
		animation.ResetTrigger(_stunTriggerExit);
		animation.SetTrigger(_stunTrigger);

		if (networkAnimation && networkAnimation.isOwned)
        {
			networkAnimation.ResetTrigger(_stunTriggerExit);
			networkAnimation.SetTrigger(_stunTrigger);
		}

		if (characterState.TryGetComponent<StateEffects>(out StateEffects stateEffects)) stateEffects.RopeTrap.SetActive(true);

		_duration = durationToExit;
		_baseDuration = durationToExit;
	}

	public void NotifyTrapDestroyed()
	{
		if (_stateClosing) return;
		_stateClosing = true;
		_spawnedTrap = null;
		ExitState();
	}

	public override void UpdateState()
	{
		_duration -= Time.deltaTime;
		if (_duration < 0 || turnOff) ExitState();
	}

	public override void ExitState()
	{
		_stateClosing = true;
		if (_spawnedTrap) NetworkServer.Destroy(_spawnedTrap);
		characterState.RemoveState(this);
		if (!characterState.Check(StatusEffect.Move)) characterState.Character.Move.IsMoveBlocked = false;
		if (!characterState.Check(StatusEffect.Ability) && abilities != null) foreach (var skill in abilities.Abilities) if (skill.Info.Moving == Moving.NonStatic) skill.Disactive = false;
		if (characterState.TryGetComponent<StateEffects>(out StateEffects stateEffects)) stateEffects.RopeTrap.SetActive(false);

		var animator = characterState.Character.Animator;
		var netAnimator = characterState.Character.NetworkAnimator;

		animator.ResetTrigger(_stunTrigger);
		animator.SetTrigger(_stunTriggerExit);
		if (netAnimator && netAnimator.isOwned)
        {
			characterState.Character.Animator.SetTrigger(_stunTriggerExit);
			characterState.Character.NetworkAnimator.SetTrigger(_stunTriggerExit);
		}

		if (!characterState.Check(StatusEffect.Ability) && abilities != null) foreach (var skill in abilities.Abilities) skill.Disactive = false;
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

