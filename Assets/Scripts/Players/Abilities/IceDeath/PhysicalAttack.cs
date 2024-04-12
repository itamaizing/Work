using System.Collections;
using System.Collections.Generic;
using GlobalEvents;
using Players.Abilities.Genjalf;
using Players.Abilities.Genjalf.Shield_Ability;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class PhysicalAttack : AbilityBase
{
	public delegate void PhysicalAttackHandler(float value);

	public event PhysicalAttackHandler PhysicalAttackEvent;

	private GameObject Target;
	private bool _isOneChange;
	//private float _chanceCriticalAttack = 0.05f;
	private Toggle _toggleFirstAbility;
	private bool _isDamageCooldownRunning = false;

	protected override KeyCode ActivationKey => KeyCode.Alpha1;

	private void Start()
	{
		Distance = _cellSize * CellDistance;
		AttackType = AttackType.Autoattack;
		AbilityType = AbilityType.DamageAbility;
		AttackRangeType = AttackRangeType.MeleeAttack;
		DamageType = DamageType.Physical;
	}

	private void Update()
	{
		Target = TargetParent;
	
		/*if (_toggleFirstAbility == null && _playerAbility != null)
		{
			_toggleFirstAbility = _playerAbility.GetComponent<OneMeleeAttack>().ToggleAbility;
		}*/

		HandleToggleAbility();
	}

	protected override void HandleToggleAbility()
	{
		base.HandleToggleAbility();

		// Текущий код в методе Update
		if (_toggleFirstAbility != null && !_toggleFirstAbility.isOn && !ToggleAbility.isOn)
		{
			TargetParent = null;
			_isOneChange = false;
		}

		if (_toggleFirstAbility != null && _toggleFirstAbility.isOn)
		{
			_isOneChange = false;
		}

		if (Input.GetMouseButtonDown(0) && _player.GetComponent<PlayerMove>().IsSelect &&
			Abilities.gameObject.activeSelf && ToggleAbility.enabled)
		{
			HandleLeftMouseButtonToggle();
		}

		if (Input.GetMouseButtonDown(1) && _player.GetComponent<PlayerMove>().IsSelect &&
			Abilities.gameObject.activeSelf)
		{
			HandleRightMouseButtonToggle();

			if (AbilityTypeManager.ActiveAbilityType == 1 &&
				_playerAbility.GetComponent<FourMeleeAttack>().ToggleAbility.isOn == false && ToggleAbility.enabled)
			{
				if (_castCoroutine != null)
				{
					ToggleAbility.isOn = false;
					return;
				}
				else
				{
					HandleAbilityType();
				}
			}
		}
	}

	protected override void HandleToggleAbilityOn()
	{
		// Включенный ToggleAbility
		base.HandleToggleAbilityOn();

		/*if (_playerAbility.GetComponent<OneMeleeAttack>().TargetParent != null)
		{
			TargetParent = _playerAbility.GetComponent<OneMeleeAttack>().TargetParent;

			if (_isOneChange == false)
			{
				ChangeBoolAndValues();
			}
		}*/

		if (TargetParent == null)
		{
			HandlePrefabVisibility();
			HandleTargetSelection();
		}

		if (TargetParent != null)
		{
			HandleDistanceToTarget();
		}
		Distance = _cellSize * CellDistance;
	}

	protected override void HandleToggleAbilityOff()
	{
		// Выключенный ToggleAbility
		base.HandleToggleAbilityOff();

		CanDealDamageOrHeal = false;
		CanMakeDamage = false;
	}

	public override void OnLeftDoubleClick()
	{
		if (ShouldUseToggleTarget() || _isInputDoubleClick)
		{
			StartCoroutine(ToggleDoubleClick());
		}
	}

	public override void OnRightDoubleClick()
	{
		StartCoroutine(DoNotDoubleClickAtTarget());
	}

	public override void ChangeBoolAndValues()
	{
		_targetHealth = TargetParent.GetComponent<HealthPlayer>();
		CanMakeDamage = true;
		CanDealDamageOrHeal = true;
		_isOneChange = true;
		Destroy(NewAbilityPrefab);
	}

	private void HandleTargetSelection()
	{
		// Выбор врага
		_targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		RaycastHit2D hit = Physics2D.Raycast(_targetPosition, Vector2.zero);

		if (hit.collider != null && hit.collider.CompareTag("Enemies") && hit.collider.gameObject != gameObject &&
			hit.collider.GetComponent<Uterus>() == null)
		{
			TargetParent = hit.collider.gameObject;
			ChangeBoolAndValues();
			if (NewAbilityPrefab != null)
			{
				Destroy(NewAbilityPrefab);
			}
		}
	}

	public override void HandleDealDamageOrHeal()
	{
		// Нанесение урона переделать
		_damageValue = Random.Range(11, 14);

		if (CanMakeDamage && _castCoroutine == null && CanUseAbility)
		{
			if (Abilities.GetComponent<GlobalCooldown>())
			{
				Abilities.GetComponent<GlobalCooldown>().StartGlobalCooldown();
			}

			/*if (Random.value < _chanceCriticalAttack)
			{
				_damageValue *= 1.6f;
			}*/
			PhysicalAttackEvent?.Invoke(_damageValue);
			
			Shield _shield = TargetParent.GetComponentInChildren<Shield>();

			if (_shield != null)
			{
				_shield.DamageInShield(_damageValue);
				_player.GetComponent<PsionicaMelee>().MakePsionica(_damageValue);

				float activePsionica = _playerAbility.GetComponent<FiveConversion>().PsionicaActive;

				if (activePsionica > 0)
				{
					HandleActivePsionica(_damageValue, activePsionica);
				}
				_castCoroutine = StartCoroutine(DamageCooldown());
			}
			else
			{
				_targetHealth.TakeDamage(_damageValue, DamageType, AttackRangeType);
//переделать
				float activePsionica = _playerAbility.GetComponent<FiveConversion>().PsionicaActive;

				if (activePsionica > 0)
				{
					HandleActivePsionica(_damageValue, activePsionica);
				}
//

				_castCoroutine = StartCoroutine(DamageCooldown());
			}
		}
	}

	private void HandleActivePsionica(float damageValue, float activePsionica)
	{
		//возможно можно убрать
		StartCoroutine(DamageEnemyCooldown(activePsionica));

		//HandleEffectsOnTarget(activePsionica, TargetParent);
		HandleEffectsOnNearbyEnemies(activePsionica, damageValue);

		//GetComponent<FiveConversion>().UseActivePsionica(activePsionica, Target);
	}

	/*private void HandleEffectsOnTarget(float activePsionica, GameObject target)
	{
		// Обработка эффектов на основной цели
		if (activePsionica >= 10 && activePsionica < 20)
		{
			List<BaseEffect> buffEffects = new List<BaseEffect>();
			Component[] allEffects = target.GetComponents<Component>();

			foreach (Component effectComponent in allEffects)
			{
				if (effectComponent is BaseEffect effect && effect.Type == EffectType.Buff)
				{
					buffEffects.Add(effect);
				}
			}

			if (buffEffects.Count > 0)
			{
				for (int i = 0; i < 1; i++)
				{
					Destroy(buffEffects[i]);
				}
			}
		}

		// Перемещение к цели, если активная псионика больше или равна 30
		if (activePsionica >= 30)
		{
			MoveTowardsEnemy(target);
		}
	}*/

	private void HandleEffectsOnNearbyEnemies(float activePsionica, float damageValue)
	{
		// Обработка эффектов на других врагах в радиусе

		Collider2D[] colliders = Physics2D.OverlapCircleAll(_player.transform.position, radius);

		foreach (Collider2D collider in colliders)
		{
			if (collider.CompareTag("Enemies") && collider.gameObject != gameObject &&
				collider.gameObject != TargetParent)
			{
				StartCoroutine(DamageEnemiesCooldown(activePsionica, collider));
				// Обработка эффектов на других врагах
				//HandleEffectsOnTarget(activePsionica, collider.gameObject);

			}
		}
	}

	/*void MoveTowardsEnemy(GameObject Target)
	{
		float distanceFromPlayer = _cellSize;
		float moveSpeed = 15f;

		Vector3 directionToPlayer = _player.transform.position - Target.transform.position;
		Vector3 normalizedDirection = directionToPlayer.normalized;
		Vector3 targetPosition = Target.transform.position - normalizedDirection * distanceFromPlayer;

		StartCoroutine(MoveTowardsCoroutine(Target, targetPosition, moveSpeed));
	}*/

	private IEnumerator DamageCooldown()
	{
		if (_isDamageCooldownRunning)
		{
			yield break; // Если корутина уже выполняется, просто выходим
		}

		_isDamageCooldownRunning = true;
		CanMakeDamage = false;

		yield return new WaitForSeconds(1.4f);

		CanMakeDamage = true;
		_castCoroutine = null;
		_isDamageCooldownRunning = false;
	}

	private IEnumerator DamageEnemyCooldown(float activePsionica)
	{
		yield return new WaitForSeconds(0.1f);
		// Нанесение урона основной цели
		_targetHealth.TakeDamage(activePsionica * 0.3f, DamageType.Magical, AttackRangeType.Inner);
	}

	private IEnumerator DamageEnemiesCooldown(float activePsionica, Collider2D collider)
	{
		yield return new WaitForSeconds(0.1f);
		// Нанесение урона врагам в радиусе
		collider.GetComponent<HealthPlayer>().TakeMagicDamage(activePsionica * 0.5f);
	}
}
