using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class IcePuddleObject : MonoBehaviour
{
	//[HideInInspector] public GameObject dadGm;
	[HideInInspector] public FrostingFrozenTalant talant;
	[HideInInspector] public float timeToDestroy = 3;

	[FormerlySerializedAs("energyPlayer")]  private Energy _energy;
	[FormerlySerializedAs("healthPlayer")]  private HealthComponent _healthComponent;
	[SerializeField] private Rigidbody2D _rb;
	[SerializeField] private SpriteRenderer _spriteRenderer;
	[SerializeField] private GameObject _hitEffect;

	 private Character _dad;
	private List<CharacterState> _enemies = new List<CharacterState>();
	private bool _initialized = false;
	/*
	 * timer to destroy
	 * buff player
	 * */
	public void Init(GameObject dad)
	{
		_dad = dad.GetComponent<Character>();
		_initialized = true;

		_energy = (Energy)_dad.Stamina;
		_healthComponent = _dad.Health;

		AfterInit();
		/*_spriteRenderer.DOFade(1, 1);
		//energy.test();
		int timeToAdd = (int)energy.Value / 5;
		if (timeToAdd > 4)
			timeToAdd = 4;

		timeToDestroy += timeToAdd;
		energy.Use(timeToAdd * 5);
		StartCoroutine(DestroyShadow());
		StartCoroutine(StartFade());*/
	}
	private void Start()
	{
		_spriteRenderer.DOFade(1, 1);
	}
	private void AfterInit()
	{
		//energy.test();
		int timeToAdd = (int)_energy.Value / 5;
		if (timeToAdd > 4)
			timeToAdd = 4;

		timeToDestroy += timeToAdd;
		_energy.Use(timeToAdd * 5) ;
		StartCoroutine(DestroyShadow());
		StartCoroutine(StartFade());
	}

	private void OnTriggerExit2D(Collider2D collision)
	{
		if (collision.gameObject == _dad.gameObject && _healthComponent != null)
		{
			_healthComponent.SetBoostRegen2(0);
			return;
		}		
	}
	private void OnTriggerEnter2D(Collider2D collision)
	{
		if(!_initialized) return;
		if (collision.gameObject == _dad.gameObject)
		{
			_healthComponent.SetBoostRegen2(0.01f);
			return;
		}
		if (collision.TryGetComponent<Character>(out var target) && _energy != null && collision.gameObject != _dad.gameObject)
		{
			float duration = 3;
			//target.CharacterState.energy = energy;
			if(_energy.Value/5 > 4) 
			{
				duration += 4;
				_energy.Use(20);
			}
			else
			{
				duration += _energy.Value / 5;
				_energy.UseAllEnergy();
			}
			target.CharacterState.AddState(new FrostingState(), _dad, duration,0,States.Frosting);
			if (talant != null)
			{
				if (talant.IsActive)
				{
					target.CharacterState.AddState(new FrozenState(), _dad, duration, 0, States.Frozen);
				}
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
		_healthComponent.SetBoostRegen2(0);
		foreach (var target in _enemies)
		{
			target.RemoveState(States.Frosting); 
			_enemies.Remove(target);
		}
		Destroy(gameObject);
	}

	private IEnumerator DestroyShadow()
	{
		yield return new WaitForSeconds(timeToDestroy);
		Destroy(gameObject);
		//turn off energy boost
		//destroy
	}
	private IEnumerator StartFade()
	{
		yield return new WaitForSeconds(timeToDestroy-2);
		_spriteRenderer.DOFade(0, 2);
		//turn off energy boost
		//destroy
	}
}
