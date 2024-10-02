using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class IcePuddleObject : Projectiles
{
	//[HideInInspector] public FrostingFrozenTalant talant;

	//[FormerlySerializedAs("energyPlayer")]  private Energy _energy;
	[FormerlySerializedAs("healthPlayer")]  private Health _healthComponent;
	//[SerializeField] private Rigidbody2D _rb;

	private float _timeToDestroy = 0;
	private bool _talentEvadeDadBoost = false;
	private bool _talentFrostingFrozen = false;
	private List<CharacterState> _enemies = new List<CharacterState>();
	/*
	 * buff player
	 * */
	public override void Init(Character dad, float timeToDestroy, bool lastHit, Skill skill)
	{
		_dad = dad;
		_skill = skill;
		_initialized = true;
		_lastHit = lastHit;
		_healthComponent = _dad.Health;
		_timeToDestroy += timeToDestroy;
		if(_lastHit)
		{
			transform.localScale = Vector3.one * 1.7f;
		}

		StartCoroutine(DestroyShadow());
		StartCoroutine(StartFade());
	}
	public void SetTalents(bool talentEvadeDadBoost, bool talentFrostingFrozen)
	{
		_talentEvadeDadBoost= talentEvadeDadBoost;
		_talentFrostingFrozen= talentFrostingFrozen;
	}
	private void Start()
	{
		_spriteRenderer.DOFade(1, 1);
	}


	private void OnTriggerExit2D(Collider2D collision)
	{
		if (collision.gameObject == _dad.gameObject && _healthComponent != null)
		{
			Debug.LogError("fix");
			//_healthComponent.SetBoostRegen2(0);
			return;
		}
		if (collision.TryGetComponent<Character>(out var target) && collision.gameObject != _dad.gameObject)
		{
			if (_talentEvadeDadBoost)
			{
				Debug.LogError("fix");
				//_dad.Health.SetEvadeAll(-3);
			}
		}
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if(!_initialized) return;
		if (collision.gameObject == _dad.gameObject)
		{
			Debug.LogError("fix");
			//_healthComponent.SetBoostRegen2(0.01f);
			return;
		}
		if (collision.TryGetComponent<Character>(out var target) && _energy != null && collision.gameObject != _dad.gameObject)
		{
			float duration = 3;
			//target.CharacterState.energy = energy;
			if (_energy.CurrentValue / 5 > 4)
			{
				duration += 4;
				_energy.TryUse(20);
			}
			else
			{
				duration += _energy.CurrentValue / 5;
				_energy.UseAllEnergy();
			}
			target.CharacterState.CmdAddState(States.Frosting, duration, 0, _dad.gameObject, _skill.name);

			if (_talentFrostingFrozen)
			{
				target.CharacterState.CmdAddState(States.Frozen, duration, 0, _dad.gameObject, _skill.name);
			}
			if (_talentEvadeDadBoost)
			{
				Debug.LogError("fix");
				//_dad.Health.SetEvadeAll(3);
			}
			_enemies.Add(target.CharacterState);
		}
		//Explode();
	}
	private void Explode()
	{
		if (_hitEffect != null)
		{
			GameObject hitEffect = Instantiate(_hitEffect, transform.position, Quaternion.identity);
			Destroy(hitEffect, 5f);
		}
		Debug.LogError("fix");
		//_healthComponent.SetBoostRegen2(0);
		foreach (var target in _enemies)
		{
			target.CmdRemoveState(States.Frosting); 
			_enemies.Remove(target);
		}
		Destroy(gameObject);
	}

	private IEnumerator DestroyShadow()
	{
		yield return new WaitForSeconds(_timeToDestroy);
		Destroy(gameObject);
		//turn off energy boost
		//destroy
	}
	private IEnumerator StartFade()
	{
		yield return new WaitForSeconds(_timeToDestroy-2);
		_spriteRenderer.DOFade(0, 2);
		//turn off energy boost
		//destroy
	}
}
