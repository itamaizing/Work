using System.Collections;
using System.Collections.Generic;
using GlobalEvents;
using Players.Abilities.Genjalf;
using Players.Abilities.Genjalf.Shield_Ability;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class PhysicalAttack : Ability
{

	[SerializeField] private float _damage = 1f;
	[SerializeField] private float _abilityCooldown = 1f;
	private int _hitInARow = 0;
	private bool _isInTheRow = false;

	protected override void Cancel()
	{
		//turn off targets and etc		
	}
	protected override void Cast()
	{
		
	}

	/*protected override void HandleToggleAbilityOn()
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
		}

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
		//hit in the row...

		if (CanMakeDamage && _castCoroutine == null && CanUseAbility)
		{
			_hitInARow++;
			_isInTheRow = true;
			_timer = 0;
			if (Abilities.GetComponent<GlobalCooldown>())
			{
				Abilities.GetComponent<GlobalCooldown>().StartGlobalCooldown();
			}

			/*if (Random.value < _chanceCriticalAttack)
			{
				_damageValue *= 1.6f;
			}
			PhysicalAttackEvent?.Invoke(_damageValue);
			
			Shield _shield = TargetParent.GetComponentInChildren<Shield>();
			/*
			 * if player dont have 5 energy _hitInARow = 0; _isInTheRow = false;
			 * 

			if (_shield != null)
			{
				_shield.DamageInShield(_damageValue);
				_castCoroutine = StartCoroutine(DamageCooldown());
			}
			else
			{
				_targetHealth.TakeDamage(_damageValue, DamageType, AttackRangeType);
				_castCoroutine = StartCoroutine(DamageCooldown());
			}
			if(_hitInARow == 6)
			{
				LastHit();
			}
		}
	}

	private IEnumerator DamageCooldown()
	{
		if (_isDamageCooldownRunning)
		{
			yield break; // Если корутина уже выполняется, просто выходим
		}

		_isDamageCooldownRunning = true;
		CanMakeDamage = false;

		yield return new WaitForSeconds(_abilityCooldownTime - _abilityCooldownTime *0.05f * _hitInARow); //delay between hits

		CanMakeDamage = true;
		_castCoroutine = null;
		_isDamageCooldownRunning = false;
	}

	private void LastHit()
	{
		//if player have 5 energy
		{
			_targetHealth.TakeDamage(_damageValue * .5f, DamageType, AttackRangeType);
			//отбрасывание и стан
			
		}
		//regen 40 energy
		_hitInARow = 0;
		_isInTheRow = false;
	}*/
}
