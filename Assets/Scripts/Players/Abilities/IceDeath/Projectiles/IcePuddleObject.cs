using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

public class IcePuddleObject : Projectiles
{
	[FormerlySerializedAs("healthPlayer")]  private Health _healthComponent;

	[SerializeField] private DecalProjector decalProjector;
	private float _timeToDestroy = 0;
    private float _damageToExit = 30;
    private float _curEvade = 0;
	private bool _talentEvadeDadBoost = false;
	private bool _talentFrostingFrozen = false;
	private bool _iceDeathInIcePudleTalent = false;
	private readonly List<Character> _enemiesInZone = new();
	private Coroutine _applyCoroutine;
	private readonly WaitForSeconds _waitApplyDelay = new WaitForSeconds(0.7f);

	//private List<CharacterState> _enemies = new List<CharacterState>();
	private List<EnemyToState> _targets = new List<EnemyToState>();

	public DecalProjector Decal { get => decalProjector; set => decalProjector = value; }
	/*
	 * buff player
	 * */
	private void OnDisable()
	{
		if (!isServer) return;

		if (_applyCoroutine != null)
		{
			StopCoroutine(_applyCoroutine);
			_applyCoroutine = null;
		}

		_enemiesInZone.Clear();
	}

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
		for (int i = 0; i < _dad.Resources.Count; i++)
		{
			if (_dad.Resources[i].Type == ResourceType.Energy)
			{
				_energy = (Energy)_dad.Resources[i];
			}
		}
		//StartCoroutine(DestroyPuddle());
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

	[Server]
	private void OnTriggerExit(Collider collision)
	{
		if (!collision.TryGetComponent<Character>(out var target)) return;
		if (!_enemiesInZone.Contains(target)) return;

		_enemiesInZone.Remove(target);

		if (_talentEvadeDadBoost && _enemiesInZone.Count == 0)
		{
			SetEvade(_dad.gameObject, -_curEvade);
			_curEvade = 0;
		}

		if (_enemiesInZone.Count == 0 && _applyCoroutine != null)
		{
			StopCoroutine(_applyCoroutine);
			_applyCoroutine = null;
		}
	}

	[Server]
	private void OnTriggerEnter(Collider collision)
	{
		if (!_initialized || !collision.TryGetComponent<Character>(out var target)) return;
		if (target == _dad) return;
		if (target.gameObject.layer == LayerMask.NameToLayer("Allies")) return;
		if (_enemiesInZone.Contains(target)) return;

		_enemiesInZone.Add(target);

		if (_talentFrostingFrozen && target.CharacterState.CheckForState(States.Frosting))
		{
			target.CharacterState.AddState(States.Frozen, _timeToDestroy, 30 + target.Health.SumDamageTaken, _dad.gameObject, _skill.name);
		}

		if (_talentEvadeDadBoost && _curEvade == 0)
		{
			_curEvade = 3;
			SetEvade(_dad.gameObject, _curEvade);
		}

		if (_applyCoroutine == null) _applyCoroutine = StartCoroutine(ApplyFrostingPeriodically());
	}

	private IEnumerator ApplyFrostingPeriodically()
	{
		while (_enemiesInZone.Count > 0)
		{
			foreach (var enemy in _enemiesInZone.ToList())
			{
				if (enemy == null) continue;
				if (enemy.CharacterState == null) continue;
				if (!enemy.CharacterState.CheckForState(States.Frosting)) enemy.CharacterState.AddState(States.Frosting, _timeToDestroy, enemy.Health.SumDamageTaken + _damageToExit, _dad.gameObject, _skill.name);
			}

			yield return _waitApplyDelay;
		}

		_applyCoroutine = null;
	}

	private void Explode()
	{
		if (_hitEffect != null)
		{
			GameObject hitEffect = Instantiate(_hitEffect, transform.position, Quaternion.identity);
			Destroy(hitEffect, 5f);
		}

		if (isServer) ClientRpcSetEvade(_dad.gameObject, -_curEvade);
		_curEvade = 0;
		//_dad.Health.SetEvadeAll(-_curEvade);
		for (int i = _targets.Count - 1; i >= 0; i--) 
		{
			_targets[i].enemy.CharacterState.CmdRemoveState(States.Frosting);
			_targets.Remove(_targets[i]);
		}
		Destroy(gameObject);
	}

	private IEnumerator DestroyPuddle()
	{
		yield return new WaitForSeconds(_timeToDestroy);
		_dad.Health.SetEvadeAll(-_curEvade);
		Destroy(gameObject);
		//turn off energy boost
		//destroy
	}

	private IEnumerator StartFade()
	{
		yield return new WaitForSeconds(_timeToDestroy-2);
		//_spriteRenderer.DOFade(0, 2);
		//turn off energy boost
		//destroy
	}

	private IEnumerator AddStateToEnemy(Character enemy, float duration)
	{
		yield return new WaitForSeconds(1);
		enemy.CharacterState.AddState(States.Frosting, duration, 0, _dad.gameObject, _skill.name);
	}

	[ClientRpc]
	private void ClientRpcSetEvade(GameObject player, float value)
	{
		Debug.Log(value + " EVADERPC");
		var health = player.GetComponent<Health>();
		health.SetEvadeAll(value);
	}
	private void SetEvade(GameObject player, float value)
	{
		Debug.Log(value + " EVADE");
		var health = player.GetComponent<Health>();
		health.SetEvadeAll(value);

		ClientRpcSetEvade(player, value);
	}

	public void IceDeathInIcePudleTalentActive(bool value)
    {
		_iceDeathInIcePudleTalent = value;
    }
}

public class EnemyToState
{
	public Character enemy;
	public float time = 0.5f;
	public float duration = 1;
	public Coroutine applyCoroutine;
}