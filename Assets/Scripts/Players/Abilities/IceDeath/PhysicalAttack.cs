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

	[SerializeField] private float _damage = 0;
	private GameObject Target;
	private bool _isOneChange;
	private int _hitCount = 0;
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

        if (ToggleAbility.isOn)
        {
			Debug.Log("first ability on");
		}
        
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

		Debug.Log("first ability off");

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
				_castCoroutine = StartCoroutine(DamageCooldown());
			}
			else
			{
				_targetHealth.TakeDamage(_damageValue, DamageType, AttackRangeType);
				_castCoroutine = StartCoroutine(DamageCooldown());
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

		yield return new WaitForSeconds(1.4f);

		CanMakeDamage = true;
		_castCoroutine = null;
		_isDamageCooldownRunning = false;
	}

}
