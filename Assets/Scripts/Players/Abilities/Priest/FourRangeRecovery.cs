using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class FourRangeRecovery : AbilityBase
{ 
	[Header("Ability properties")]
	[SerializeField] private GameObject RecoveryBaffPrefab;
    [SerializeField] private GameObject DamageDebaffPrefab;
    [SerializeField] private GameObject ManaCost;
	[SerializeField] private float _castTime=1.2f;
	[SerializeField] private float _heal = 6f;

	public delegate void FourthAbilityHandler(float value);
	public event FourthAbilityHandler FourthAbilityEvent;

    public delegate void DarkFourthAbilityHandler(float value);
    public event DarkFourthAbilityHandler DarkFourthAbilityEvent;

    private bool isLightSide = true;

    private GameObject _newPrefab;
	private float _trueHeal;
	private int _healTickCount = 0;
	public bool canCast = true;

	protected override KeyCode ActivationKey => KeyCode.Alpha4;

	private void Start()
	{
		Distance = _cellSize * CellDistance;
		AttackType = AttackType.OneAttack;
		AbilityType = AbilityType.HealAbility;
	}

	void Update()
	{
		HandleToggleAbility();
	}

	protected override void HandleToggleAbility()
	{
		if (!canCast) return;
		base.HandleToggleAbility();
		// Текущий код в методе Update

		if (Input.GetMouseButtonDown(0) && _player.GetComponent<MoveComponent>().IsSelect && ToggleAbility.gameObject.activeSelf && ToggleAbility.enabled == true)
		{
			HandleLeftMouseButtonToggle();
		}

		if (Input.GetMouseButtonDown(1) && _player.GetComponent<MoveComponent>().IsSelect && ToggleAbility.gameObject.activeSelf && ToggleAbility.enabled == true)
		{
			Vector2 cursorPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			RaycastHit2D hit = Physics2D.Raycast(cursorPosition, Vector2.zero);

			if (hit.collider != null && hit.collider.CompareTag("Allies") && hit.collider.gameObject != gameObject)
			{
				HandleRightMouseButtonToggle();
			}
		}
	}

	protected override void HandleToggleAbilityOn()
	{
        if (!canCast) return;
        // Включенный ToggleAbility
        base.HandleToggleAbilityOn();

		if (TargetParent == null)
		{
			if (ManaCost != null)
			{
				ManaCost.SetActive(true);
				ManaCost.GetComponent<VisualManaCost>().CheckManaCost();
				ManaCost.transform.localScale = new Vector2(0.4f, ManaCost.gameObject.transform.localScale.y);
			}

			HandlePrefabVisibility();
			HandleTargetSelection();
		}

		if (TargetParent != null)
		{
			if (ManaCost != null)
			{
				ManaCost.gameObject.SetActive(false);
			}

			HandleDistanceToTarget();
		}
	}

	protected override void HandleToggleAbilityOff()
	{
		// Выключенный ToggleAbility
		base.HandleToggleAbilityOff();

		if (_isSelect == false)
		{
			ManaCost.gameObject.SetActive(false);
		}

		return;
	}

	public override void OnLeftDoubleClick()
	{
        if (!canCast) return;
        if (ShouldUseToggleTarget() || _isInputDoubleClick)
		{
			StartCoroutine(ToggleDoubleClick());
		}
	}

	public override void OnRightDoubleClick()
	{
		if (AbilityTypeManager.ActiveAbilityType == 1 && _player.GetComponent<MoveComponent>().IsSelect && Abilities.gameObject.activeSelf)
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

	public override void ChangeBoolAndValues()
	{
		Destroy(NewAbilityPrefab);
	}

	private void HandleTargetSelection()
	{
		// Выбор врага
		_targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		RaycastHit2D hit = Physics2D.Raycast(_targetPosition, Vector2.zero);

		if (isLightSide && hit.collider != null && hit.collider.CompareTag("Allies") && hit.collider.gameObject != gameObject)
		{
			TargetParent = hit.collider.gameObject;

			if (NewAbilityPrefab != null)
			{
				Destroy(NewAbilityPrefab);
			}
			DrawCircle.Clear();
		}
		else if (isLightSide && hit.collider != null && hit.collider.CompareTag("Allies") && hit.collider.gameObject == gameObject)
		{
			TargetParent = transform.parent.gameObject;

			if (NewAbilityPrefab != null)
			{
				Destroy(NewAbilityPrefab);
			}
			DrawCircle.Clear();
		}
        else if (!isLightSide && hit.collider != null && hit.collider.CompareTag("Enemies") && hit.collider.gameObject != gameObject)
        {
            TargetParent = hit.collider.gameObject;

            if (NewAbilityPrefab != null)
            {
                Destroy(NewAbilityPrefab);
            }
            DrawCircle.Clear();
        }
    }

	public override void HandleDealDamageOrHeal()
	{
		if (_castCoroutine == null)
		{
			if(isLightSide && TargetParent == transform.parent.gameObject)
			{
				_castCoroutine = StartCoroutine(CastProtect(0f));
			}
			else if(isLightSide && TargetParent != transform.parent.gameObject)
			{
                _castCoroutine = StartCoroutine(CastProtect(_castTime));
            }
			else
			{
				_castCoroutine = StartCoroutine(CastProtect(_castTime));
			}
		}
	}

    public void ReverseAbility(bool isLight)
    {
        ToggleAbility.isOn = false;
        isLightSide = isLight;
        _castCoroutine = null;
        if (isLightSide)
        {
            AbilityType = AbilityType.HealAbility;
        }
        else
        {
            AbilityType = AbilityType.DamageAbility;
        }
    }

    private void Heal()
	{
		if (_newPrefab != null && TargetParent.GetComponentInChildren<HealthRecovery>() != null) // обновление спелла
		{
			TargetParent.GetComponentInChildren<HealthRecovery>().Timer = Time.time;
            TargetParent.GetComponentInChildren<HealthRecovery>().isRecovery = false;
			TargetParent.GetComponentInChildren<BaffDebaffEffectPrefab>().StartCountdown(12);
		}
		else if (TargetParent != null) // если на цели нет бафа
		{
            _newPrefab = null;
			_newPrefab = Instantiate(RecoveryBaffPrefab);
			_newPrefab.transform.SetParent(TargetParent.transform);
			_newPrefab.GetComponentInChildren<BaffDebaffEffectPrefab>().StartCountdown(12);
			_newPrefab.GetComponent<HealthRecovery>().CastRecovery(12f, _heal, 4f,_player);
		}
		_player.GetComponent<Mana>().Use(4f);
		FourthAbilityEvent?.Invoke(0f);
		Recharge();
	}

    private void Damage()
    {
        if (_newPrefab != null && TargetParent.GetComponentInChildren<Damage>()!=null)
        {
            TargetParent.GetComponent<Damage>().Timer = Time.time;
			TargetParent.GetComponentInChildren<Damage>().isDamage = false;
            TargetParent.GetComponentInChildren<BaffDebaffEffectPrefab>().StartCountdown(12);
        }
        else if(TargetParent!=null)
        {
			_newPrefab = null;
            _newPrefab = Instantiate(DamageDebaffPrefab);
            _newPrefab.transform.SetParent(TargetParent.transform);
            _newPrefab.GetComponentInChildren<BaffDebaffEffectPrefab>().StartCountdown(12);
            _newPrefab.GetComponent<Damage>().CastRecovery(12f, 6f, 3f , _player);
        }
        _player.GetComponent<Mana>().Use(4f);

        DarkFourthAbilityEvent?.Invoke(0f);
        Recharge();
    }

    public int CheckSpriritStacks(bool isLight)
	{
		int stacks = 0;
		if (isLight)
		{
			stacks = _player.GetComponentInChildren<OneRangeAttack>().SpiritBaffCount;
		}
		else
		{
            stacks = _player.GetComponentInChildren<OneRangeAttack>().SpiritDebaffCount;
        }

		return stacks;
    }

	private IEnumerator CastProtect(float castTime)
	{
		for (int i = 0; i < Abilities.transform.childCount; i++)
		{
			GameObject childObject = Abilities.transform.GetChild(i).gameObject;

			Toggle toggle = childObject.GetComponent<Toggle>();

			if (toggle != null)
			{
				toggle.enabled = false;
			}
		}
		_player.GetComponent<MoveComponent>().CanMove = false;
		CreateCastPrefab(castTime);

		yield return new WaitForSeconds(castTime);
		_castCoroutine = null;
		_player.GetComponent<MoveComponent>().CanMove = true;
		if(isLightSide)
		{
            Heal();
        }
		else
		{
			Damage();
            GetComponent<FiveReversPolarity>().UseReversePolarity(0f);
        }
        lastAbility.AddLastAbility(this);
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
		Select.GetComponent<SelectObject>().CanSelect = true;
		ToggleAbility.isOn = false;
		TargetParent = null;
    }
}