using System.Collections;
using GlobalEvents;
using Players.Abilities.Genjalf;
using Players.Abilities.Genjalf.Shield_Ability;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class IceRolling : AbilityBase
{
	[Header("Ability properties")]
	[SerializeField]
	private Renderer[] _renderers;

	//[HideInInspector] public GameObject Target;

	public delegate void ThirdAbilityHandler(float value);

	public event ThirdAbilityHandler ThirdAbilityEvent;

	[SerializeField] private Rigidbody2D _rb;
	[SerializeField] private EnergyPlayer _energy;
	[SerializeField] private float _jumprange = 2f;

	private Vector2 _jumpPos;
	private bool _canJump = true;
	protected override KeyCode ActivationKey => KeyCode.Alpha5;

	private void Start()
	{
		Distance = 5f * 1.9f;
		AttackType = AttackType.OneAttack;
		AbilityType = AbilityType.DamageAbility;
		AttackRangeType = AttackRangeType.MeleeAttack;
		DamageType = DamageType.Magical;
	}

	private void Update()
	{
		HandleToggleAbility();
	}

	protected override void HandleToggleAbility()
	{
		base.HandleToggleAbility();

		if (Input.GetMouseButtonDown(0) && ToggleAbility.gameObject.activeSelf && ToggleAbility.enabled &&
			_player.GetComponent<PlayerMove>().IsSelect)
		{
			HandleLeftMouseButtonToggle();
		}
	}

	public override void CancelAbilityOnClick()
	{
		StartCoroutine(Stop());
		base.CancelAbilityOnClick();
	}
	protected override void HandleToggleAbilityOn()
	{
		if(Input.GetMouseButtonDown(0)) 
		{
			//переделать круг
			base.HandleToggleAbilityOn();
			HandleCastJump();
		}
	}

	protected override void HandleToggleAbilityOff()
	{
		// Выключенный ToggleAbility
		base.HandleToggleAbilityOff();
		_castCoroutine = null;
		_canJump = false;
	}

	public override void OnLeftDoubleClick()
	{
		if (ShouldUseToggleTarget() || _isInputDoubleClick)
		{
			StartCoroutine(ToggleDoubleClick());
		}

		else if (AbilityTypeManager.ActiveAbilityType == 1 && _player.GetComponent<PlayerMove>().IsSelect &&
				 Abilities.gameObject.activeSelf)
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

	public override void OnRightDoubleClick()
	{
	}

	public override void ChangeBoolAndValues()
	{
		if (NewAbilityPrefab != null)
		{
			Destroy(NewAbilityPrefab);
		}
	}

	public override void HandleDealDamageOrHeal()
	{
		HandleCastJump();
		Destroy(NewAbilityPrefab);
	}

	private void HandleCastJump()
	{
		_castCoroutine = StartCoroutine(CastJump());		
	}

	private void Jump()
	{
		if (_canJump && ToggleAbility.isOn == true)
		{
			_canJump = false;
			ToggleAbility.enabled = false;
			float actualJumpRange = _jumprange;

			Vector2 _mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			Vector2 lookDir = (_mousePos - _rb.position).normalized;
			
			if(_energy.Energy >= 10)
			{
				actualJumpRange += 2;
			}
			else if(_energy.Energy < 10 && _energy.Energy >=5)
			{
				actualJumpRange += 1;
			}	
			
			Vector2 jumpPos = lookDir * actualJumpRange + (Vector2)_player.transform.position;
			if(CheckObstacleBetween(_rb.position, jumpPos))
			{
				Debug.Log("Обнаружено препятствие:");
				//прыгать до препятствия
				_rb.DOMove(_jumpPos, 0.3f * actualJumpRange);
			}
			else
			{
				_energy.UseEnergy((actualJumpRange - _jumprange) * 5);
				_rb.DOMove(jumpPos, 0.3f * actualJumpRange);
			}
			HandleToggleAbilityOff();
		}
	}

	private IEnumerator CastJump()
	{
		_player.GetComponent<PlayerMove>().CanMove = false;


		if (Abilities.GetComponent<GlobalCooldown>())
		{
			Abilities.GetComponent<GlobalCooldown>().StartGlobalCooldown();
		}

		yield return new WaitForSeconds(0.3f);

		_player.GetComponent<PlayerMove>().CanMove = true;
		_canJump = true;

		Jump();
	}

	private bool CheckObstacleBetween(Vector3 start, Vector3 end)
	{
		//Проверка на наличие препятствия
		Vector2 direction = (end - start).normalized;
		float distance = Vector2.Distance(start, end);

		RaycastHit2D[] hits =
			Physics2D.BoxCastAll(start, new Vector2(1f, 1f), 0f, direction, distance, ObstacleLayerMask);

		foreach (RaycastHit2D hit in hits)
		{
			_jumpPos = hits[0].point - direction;
			return true;
		}

		return false;
	}

	private IEnumerator Stop()
	{
		yield return new WaitForSeconds(0.3f);

		_player.GetComponent<PlayerMove>().CanMove = true;

		yield return new WaitForSeconds(0.4f);

		foreach (var item in _renderers)
		{
			item.sortingLayerID = SortingLayer.NameToID("Default");
		}

		ToggleAbility.isOn = false;
		ToggleAbility.enabled = true;
		_canJump = false;
		StopBackgroundSwitcherEvent.SendStartStopBackgroundSwitcher();
		Debug.Log("Конец атаки");
		yield break;
	}
}
