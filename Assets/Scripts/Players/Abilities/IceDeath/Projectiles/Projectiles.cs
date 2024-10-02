using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Projectiles : MonoBehaviour
{
	[SerializeField] protected GameObject _hitEffect;
	[SerializeField] protected SpriteRenderer _spriteRenderer;
	[SerializeField] protected Rigidbody2D _rb;
	[SerializeField] protected float _force = 0;
	[SerializeField] protected float _distance = 5;
	protected Character _dad;
	protected Skill _skill;
	protected Energy _energy;
	protected bool _initialized = false;
	protected float _energyDad = 0;
	protected bool _lastHit = false;

	public virtual void Init(Character dad, float energy, bool lastHit, Skill skill)
	{
		_dad = dad;
		_energyDad = energy;
		_initialized = true;
		_lastHit = lastHit;
		_skill = skill;
		_rb.AddForce(transform.up * _force, ForceMode2D.Impulse);
		for (int i = 0; i < _dad.Resources.Count; i++)
		{
			if (_dad.Resources[i].Type == ResourceType.Energy)
			{
				_energy = (Energy)_dad.Resources[i];
			}
		}
		Debug.Log("bullet init");
	}
}
