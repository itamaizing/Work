using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Bound : AbstractCharacterState
{
	public bool turnOff = false;
	private float _baseDuration;
	private static readonly int _stunTrigger = Animator.StringToHash("Rope");
	private static readonly int _stunTriggerExit = Animator.StringToHash("RopeExit");
	private GameObject _spawnedTrap;
	private bool _stateClosing;

	private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move, StatusEffect.Ability };
	public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
	public override States State => States.Bound;
	public override StateType Type => StateType.Physical;
	public override List<StatusEffect> Effects => _effects;


    protected override void OnEnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
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

		if (character.isServer && character.StateEffects.TrapPrefab)
		{
			characterState = character;
			character.StartCoroutine(ServerSpawnTrapNextFrame());
		}

		if (characterState.TryGetComponent<StateEffects>(out StateEffects stateEffects)) stateEffects.RopeTrap.SetActive(true);

		_baseDuration = durationToExit;
	}

	public void NotifyTrapDestroyed()
	{
		if (_stateClosing) return;
		_stateClosing = true;
		_spawnedTrap = null;
		OnExitState();
	}

	public override void OnUpdateState()
	{
		if (turnOff) ExitState();
	}

	protected override void OnExitState()
	{
		_stateClosing = true;
		if (_spawnedTrap) NetworkServer.Destroy(_spawnedTrap);
		characterState.RemoveStateFromList(this);
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

	/*public override bool Stack(float time)
	{
		if (_baseDuration > time) return false;
		else
		{
			_duration = time;
			return true;
		}
	}*/

	[Server]
	private IEnumerator ServerSpawnTrapNextFrame()
	{
		yield return null;

		var character = characterState.Character;

		Vector3 position = character.transform.position;

		if (Physics.Raycast(position + Vector3.up * 2f, Vector3.down, out var hit, 5f))
			position = hit.point;

		Quaternion rot = Quaternion.LookRotation(character.transform.forward, Vector3.up);

		_spawnedTrap = GameObject.Instantiate(characterState.StateEffects.TrapPrefab, position, rot);

		var life = _spawnedTrap.GetComponent<TrapStateLife>();
		life.Init(character.gameObject);

		SceneManager.MoveGameObjectToScene(_spawnedTrap.gameObject, character.NetworkSettings.MyRoom);
		NetworkServer.Spawn(_spawnedTrap);
	}
}

