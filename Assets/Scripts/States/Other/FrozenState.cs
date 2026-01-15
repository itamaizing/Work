using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrozenState : IndependentState
{
	//public bool turnOff = false;
	private GameObject _frozenEffectInstance;
	private AudioSource _audioSource;
	private float _damageToExit;
	private float _damageOnStart = 0;
	private bool _isInited = false;

	private Animator _animator;
	private AnimatorStateInfo _currentState;

	private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.Ability };
	public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
	public override States State => States.Frozen;
	public override StateType Type => StateType.Magic;
	public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		MaxStacksCount = 3;
		if (damageToExit == 0)
		{
			_damageToExit = 10000;
		}
		else
		{
			_damageToExit = damageToExit;
		}

		//_damageOnStart = _characterState.Character.Health.SumDamageTaken;
		_damageOnStart = 0;
		characterState.Character.Move.CanMove = false;
		characterState.Character.Move.Rigidbody.isKinematic = true;
		characterState.Character.Move.LookAtTransform(characterState.gameObject.transform);
		_audioSource = character.GetComponent<AudioSource>();

		if (character.TryGetComponent<Character>(out var ability))
		{
			abilities = ability.Abilities;

			foreach (var abil in abilities.Abilities)
			{
				abil.Disactive = true;
				if (abil.AbilityForm == AbilityForm.Physical)
				{
					abil.Buff.CastSpeed.ReductionPercentage(.5f);
				}
			}
		}
		else
		{
			Debug.Log("no ability at " + character.gameObject.name);
		}
		if (characterState.StateEffects.FrozenStateEffect != null)
		{
			_frozenEffectInstance = characterState.StateEffects.FrozenStateEffect;
			_frozenEffectInstance.SetActive(true);
		}

		foreach (var mat in characterState.StateEffects.MaterialsCharacter) mat.color = Color.cyan;
		if (characterState.StateEffects.FrostingAudio != null) _audioSource.PlayOneShot(characterState.StateEffects.FrozenAudio);

		_animator = characterState.GetComponent<Animator>();

		if (_animator != null)
		{
			_currentState = _animator.GetCurrentAnimatorStateInfo(0);
			float normalizedTime = _currentState.normalizedTime % 1f;
			_animator.Play(_currentState.fullPathHash, 0, normalizedTime);
			_animator.Update(0);
			_animator.enabled = false;
		}
		_isInited = true;
	}

	public override void UpdateState()
	{		
		if(!_isInited) return;
		if (characterState.Character.Health.SumDamageTaken - _damageOnStart >= _damageToExit )//|| turnOff)
		{
			ExitState();
		}
	}

	public override void ExitState()
	{
		//Debug.Log("Exiting Frozen State");

		characterState.RemoveState(this);
		if (!characterState.Check(StatusEffect.Move))
		{
			characterState.Character.Move.CanMove = true;
			characterState.Character.Move.Rigidbody.isKinematic = false;
			characterState.Character.Move.StopLookAt();
		}
		if (!characterState.Check(StatusEffect.Ability) && abilities != null)
		{
			foreach (var abil in abilities.Abilities)
			{
				abil.Disactive = false;
			}
		}

		if (_frozenEffectInstance != null) _frozenEffectInstance.SetActive(false);
		foreach (var mat in characterState.StateEffects.MaterialsCharacter) mat.color = Color.white;

		if (_animator != null)
		{
			_animator.enabled = true;
			_animator.speed = 1;
		}
	}
}
