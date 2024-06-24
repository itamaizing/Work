using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectiles : MonoBehaviour
{
	[SerializeField] protected GameObject _hitEffect;
	[SerializeField] protected SpriteRenderer _spriteRenderer;
	[SerializeField] protected Rigidbody2D _rb;
	[SerializeField] protected float _force;
	[SerializeField] protected float _distance = 5;
	protected Character _dad;
	protected bool _initialized = false;
	protected float _energyDad = 0;
	protected bool _lastHit = false;

	public virtual void Init(Character dad, float energy, bool lastHit)
	{
		_dad = dad;
		_energyDad = energy;
		_initialized = true;
		_lastHit = lastHit;
		Debug.Log("bullet init");
	}
}
