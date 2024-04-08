using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class Icecloud : AbilityBase
{
	[Header("Ability properties")]
	[SerializeField] private GameObject ManaCost;
	[SerializeField] private Rigidbody2D _rb;
	[SerializeField] private IceCloudProjectile _projectile;
	//[HideInInspector] public GameObject Target;
	[SerializeField] private Collider2D _collider;

	public delegate void IceCloudAbilityHandler(float value);
	public event IceCloudAbilityHandler IceCloudAbilityEvent;

	protected override KeyCode ActivationKey => KeyCode.Alpha2;

	private Vector2 _mousePos;
	private PlayerMove _playerMove;

	private void Start()
	{
		//Distance = 6f * 1.9f;
		AttackType = AttackType.OneAttack;
		AbilityType = AbilityType.DamageAbility;
		AttackRangeType = AttackRangeType.RangeAttack;
	}

	void Update()
	{
		HandleToggleAbility();
		//Target = TargetParent;
	}


	protected override void HandleToggleAbility()
	{
		base.HandleToggleAbility();
		// Текущий код в методе Update

		if (Input.GetMouseButtonDown(0) && Abilities.gameObject.activeSelf && ToggleAbility.enabled)
		{
			if(_player.TryGetComponent<PlayerMove>(out _playerMove))
			{
				if(_playerMove.IsSelect)
				{
					HandleLeftMouseButtonToggle();
				}
			}
			
		}
	}

	protected override void HandleToggleAbilityOn()
	{
		// Включенный ToggleAbility
		base.HandleToggleAbilityOn();		
		if(Input.GetMouseButtonDown(0)) 
		{
			if (ManaCost != null)
			{
				ManaCost.SetActive(true);
				ManaCost.GetComponent<VisualManaCost>().CheckManaCost();
				ManaCost.transform.localScale = new Vector2(3f, ManaCost.gameObject.transform.localScale.y);
			}
			HandleDealDamageOrHeal();
		}
		/*if (TargetParent == null)
		{
			if (ManaCost != null)
			{
				ManaCost.SetActive(true);
				ManaCost.GetComponent<VisualManaCost>().CheckManaCost();
				ManaCost.transform.localScale = new Vector2(3f, ManaCost.gameObject.transform.localScale.y);
			}
			HandlePrefabVisibility();
			HandleTargetSelection();
		}

		if (TargetParent != null)
		{
			Debug.Log("Target");
			if (ManaCost != null)
			{
				ManaCost.gameObject.SetActive(false);
			}

			HandleDistanceToTarget();
		}*/
	}

	protected override void HandleToggleAbilityOff()
	{
		// Выключенный ToggleAbility
		base.HandleToggleAbilityOff();

		if (_isSelect == false)
		{
			ManaCost.gameObject.SetActive(false);
		}
		TargetParent = null;
		return;
	}

	public override void OnLeftDoubleClick()
	{
		if (ShouldUseToggleTarget() || _isInputDoubleClick)
		{
			StartCoroutine(ToggleDoubleClick());
		}

		else if (AbilityTypeManager.ActiveAbilityType == 1 && _playerMove.IsSelect && Abilities.gameObject.activeSelf)
		{
			if (_castCoroutine != null)
			{
				ToggleAbility.isOn = false;
				return;
			}
			else
			{
				StartCoroutine(EnemiesDoubleClick());
			}
		}
	}

	public override void OnRightDoubleClick()
	{
	}

	public override void ChangeBoolAndValues()
	{
		Destroy(NewAbilityPrefab);
	}

	/*private void HandleTargetSelection()
	{
		// Выбор врага
		_targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		RaycastHit2D hit = Physics2D.Raycast(_targetPosition, Vector2.zero);

		if (hit.collider != null && hit.collider.CompareTag("Enemies") && hit.collider.gameObject != gameObject)
		{
			TargetParent = hit.collider.gameObject;

			if (NewAbilityPrefab != null)
			{
				Destroy(NewAbilityPrefab);
			}
			DrawCircle.Clear();
		}
	}*/

	public override void HandleDealDamageOrHeal()
	{
		if (_castCoroutine == null)
		{
			_castCoroutine = StartCoroutine(CastProtect(0));
		}
	}

	private void Damage()
	{
		//to projectile

		if (TargetParent != null)
		{
			TargetParent.GetComponent<HealthPlayer>().TakeMagicDamage(35f);
			//_player.GetComponent<ManaPlayer>().UseMana(30f);


			//freeze

			IceCloudAbilityEvent?.Invoke(35f);
			Recharge();
		}
	}

	private IEnumerator CastProtect(float castTime)
	{
		if (!_player.GetComponent<RunePlayer>().RemoveRune(1))
		{
			yield break;
		}
		if (Abilities.GetComponent<GlobalCooldown>())
		{
			Abilities.GetComponent<GlobalCooldown>().StartGlobalCooldown();
		}		
		for (int i = 0; i < Abilities.transform.childCount; i++)
		{
			GameObject childObject = Abilities.transform.GetChild(i).gameObject;

			Toggle toggle = childObject.GetComponent<Toggle>();

			if (toggle != null)
			{
				toggle.enabled = false;
			}
		}
		_playerMove.CanMove = false;
		CreateCastPrefab(castTime);

		yield return new WaitForSeconds(castTime);

		_mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		Vector2 lookDir = _mousePos - _rb.position;
		float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
		IceCloudProjectile projectile = Instantiate(_projectile, gameObject.transform.position, Quaternion.Euler(0, 0, angle));
		projectile.dad = _rb.gameObject;

		_castCoroutine = null;
		_playerMove.CanMove = true;
		Select.GetComponent<SelectObject>().CanSelect = true;

		IceCloudAbilityEvent?.Invoke(35f);
		Recharge();
	}

	private IEnumerator EnemiesDoubleClick()
	{
		yield return new WaitForSeconds(0.1f);

		ToggleAbility.isOn = true;
		HandleAbilityType();
	}


	private void Recharge()
	{
		for (int i = 0; i < Abilities.transform.childCount; i++)
		{
			GameObject childObject = Abilities.transform.GetChild(i).gameObject;

			Toggle toggle = childObject.GetComponent<Toggle>();

			if (toggle != null)
			{
				toggle.enabled = true;
			}
		}
		ToggleAbility.isOn = false;
		TargetParent = null;
		return;
	}
}
