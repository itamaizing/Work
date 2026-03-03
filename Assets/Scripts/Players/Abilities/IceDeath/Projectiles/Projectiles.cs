using Mirror;
using UnityEngine;


public class Projectiles : NetworkBehaviour
{
	[SerializeField] protected GameObject _hitEffect;
	[SerializeField] protected SpriteRenderer _spriteRenderer;
	[SerializeField] protected Rigidbody _rb;
	[SerializeField] protected float _force = 0;
	[SerializeField] protected float _distance = 5;
	protected Character _dad;
	protected Skill _skill;
	protected Energy _energy;
	protected RuneComponent _rune;
	protected bool _initialized = false;
	protected float _energyDad = 0;
	protected bool _lastHit = false;

	public Rigidbody Rigidbody {get => _rb; set => _rb = value;}

	public virtual void Init(Character dad, float energy, bool lastHit, Skill skill)
	{
		_dad = dad;
		_energyDad = energy;
		_initialized = true;
		_lastHit = lastHit;
		_skill = skill;
		_rb.AddForce(transform.forward * _force, ForceMode.Impulse);
        if (_dad.Resources.TryGetValue(ResourceType.Energy, out var res))
			_energy = (Energy) res;
        if (_dad.Resources.TryGetValue(ResourceType.Rune, out res))
            _rune = (RuneComponent)res;
    }

    [ClientRpc]
	protected void TargetRpcDamageMake(float value)
	{
		//Debug.Log("CLIENT RPC");
		_energy.SumDamageMake(value);
		_rune.SumDamageMake(value);
	}

}
