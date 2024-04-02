using System.Collections;
using UnityEngine;

public class OneRangeAttack : AbilityBase
{
	[HideInInspector] public static int NumberOfInstances = 0;
	[HideInInspector] public float Heal = 2f;
	[HideInInspector] public int ScriptInstanceCount = 0;
	[HideInInspector] public GameObject Target;

	[Header("Ability properties")]
	[SerializeField] private GameObject EnergySpiritEffect;
	[SerializeField] private GameObject ManaCost;
	[SerializeField] private float CastTime;
    [SerializeField] private float _countdownForEnergyOfSpirit;
    public delegate void FirstAbilityHandler(float value);
	public event FirstAbilityHandler FirstAbilityEvent;
	public event System.Action<EnergyOfSpirit> ScriptInstanceDestroyed;

	private int maxScriptInstances = 2;
	private GameObject _newPrefab;
	private bool _canCast;
	protected float shieldBuff;

    protected override KeyCode ActivationKey => KeyCode.Alpha1;


	private void Start()
	{
		Distance = CellDistance *_cellSize;
		AttackType = AttackType.Autoattack;
		AbilityType = AbilityType.HealAbility;
	}

	void Update()
	{
		HandleToggleAbility();
	}

	protected override void HandleToggleAbility()
	{
		base.HandleToggleAbility();
		// Текущий код в методе Update

		if (Input.GetMouseButtonDown(0) && _player.GetComponent<PlayerMove>().IsSelect && ToggleAbility.gameObject.activeSelf)
		{
			HandleLeftMouseButtonToggle();
			if (AbilityTypeManager.ActiveAbilityType == 1)
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

		if (TargetParent == null)
		{
			if (ManaCost != null)
			{
				ManaCost.SetActive(true);
				ManaCost.GetComponent<VisualManaCost>().CheckManaCost();
				ManaCost.transform.localScale = new Vector2(0.1f, ManaCost.gameObject.transform.localScale.y);
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
        TargetParent = null;
		_canCast = false;
		CanDealDamageOrHeal = false;
	}

	public override void OnLeftDoubleClick()
	{
		if (ShouldUseToggleTarget() || _isInputDoubleClick)
		{
			StartCoroutine(ToggleDoubleClick());
		}
		else if (AbilityTypeManager.ActiveAbilityType == 1 && _player.GetComponent<PlayerMove>().IsSelect && Abilities.gameObject.activeSelf)
		{
			StartCoroutine(DoNotDoubleClickAtTarget());
		}
	}

	public override void OnRightDoubleClick()
	{
	}

	public override void ChangeBoolAndValues()
	{
		CanDealDamageOrHeal = true;
		_canCast = true;
		Destroy(NewAbilityPrefab);
	}

	private void HandleTargetSelection()
	{
		// Выбор врага

		_targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		RaycastHit2D hit = Physics2D.Raycast(_targetPosition, Vector2.zero);

		if (hit.collider != null && hit.collider.CompareTag("Allies") && hit.collider.gameObject != gameObject)
		{
			TargetParent = hit.collider.gameObject;
			Target = TargetParent;

			CanDealDamageOrHeal = true;
			_canCast = true;

			if (NewAbilityPrefab != null)
			{
				Destroy(NewAbilityPrefab);
			}
		}
	}

	public override void HandleDealDamageOrHeal()
	{
		// Лечение
		if (_canCast && _castCoroutine == null)
		{
			_castCoroutine = StartCoroutine(Cast());
			CreateCastPrefab(CastTime);
			_canCast = false;
		}
	}

	private void Healing()
	{
		if (TargetParent != null)
		{
			TargetParent.GetComponent<HealthPlayer>().AddHeal(Heal);
			_player.GetComponent<ManaPlayer>().UseMana(1f); // ToDo вынести ману в атрибуты
			FirstAbilityEvent?.Invoke(Heal);

			if (TargetParent.GetComponentInChildren<DamageAbsorption>())
			{
				TargetParent.GetComponentInChildren<DamageAbsorption>().AddMaxAbsorbtion(shieldBuff*0.01f);
			}

			AddBaffEnergyOfSpirit();
		}
		_canCast = true;
	}

	private void AddBaffEnergyOfSpirit()
	{
		if (ScriptInstanceCount < maxScriptInstances)
		{
			_newPrefab = Instantiate(EnergySpiritEffect);
			EnergyOfSpirit newScript = _newPrefab.GetComponent<EnergyOfSpirit>();
			newScript.Destroyed += OnScriptInstanceDestroyed;

			_newPrefab.transform.SetParent(TargetParent.transform);
			_newPrefab.GetComponentInChildren<BaffDebaffEffectPrefab>().StartCountdown(_countdownForEnergyOfSpirit);
			ScriptInstanceCount++;
			float manaBuffPercent = ScriptInstanceCount*0.1f;
            newScript.SetPriestAbilityManaReduce(manaBuffPercent);
            ShieldBuff();
            TargetParent.GetComponent<HealthPlayer>().AddBuff(1);
        }
    }

	private void ShieldBuff()
	{
        switch (ScriptInstanceCount)
        {
            case 1:
                shieldBuff = 10;
                break;
            case 2:
                shieldBuff = 5;
                break;
            default:
                break;
        }

    }
	private void OnScriptInstanceDestroyed(EnergyOfSpirit destroyedScript )
	{
		ScriptInstanceDestroyed?.Invoke(destroyedScript);
        Target.GetComponent<HealthPlayer>().RemoveBuff(1); // toDo
        ScriptInstanceCount--;
	}

	private IEnumerator Cast()
	{
		_canCast = false;

		if (Abilities.activeSelf && Abilities.GetComponent<GlobalCooldown>())
		{
			Abilities.GetComponent<GlobalCooldown>().StartGlobalCooldown();
		}

		StartCoroutine(CastMove());

		yield return new WaitForSeconds(CastTime);
		Healing();

		this.transform.root.GetComponentInChildren<FourRangeRecovery>().canCast = true;

		_castCoroutine = null;
		yield break;
	}

	private IEnumerator CastMove()
	{
		GetComponentInParent<PlayerMove>().CanMove = false;
		yield return new WaitForSeconds(CastTime/2);
		GetComponentInParent<PlayerMove>().CanMove = true;

	}
}
