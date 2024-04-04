using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class OneRangeAttack : AbilityBase
{
	// "Искра Света" - Лечение и бафф союзника , урон по врагу. 
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
	private EnergyOfSpirit _energyOfSpiritPrefab;
	private bool _canCast;
	protected float shieldBuff;
	private float manaBuff;
	public float ManaBaff
	{
		get
		{
			return manaBuff;
		}
		set 
		{ 
			manaBuff = value; 
		}
	}

    protected override KeyCode ActivationKey => KeyCode.Alpha1;


	private void Start()
	{
		Distance = CellDistance *_cellSize;
		AttackType = AttackType.Autoattack;
		AbilityType = AbilityType.HealAbility;
		CanDoAbilityOnMyself = false;
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

		if (_isSelect == false	)
		{
			ManaCost.gameObject.SetActive(false);
		}
        TargetParent = null;
		_canCast = false;
		CanDealDamageOrHeal = false;
	}

	public override void OnLeftDoubleClick()
	{
        /*if (ShouldUseToggleTarget() || _isInputDoubleClick)
        {
            StartCoroutine(ToggleDoubleClick());
        }
        else if (AbilityTypeManager.ActiveAbilityType == 1 && _player.GetComponent<PlayerMove>().IsSelect && Abilities.gameObject.activeSelf)
        {
            StartCoroutine(DoNotDoubleClickAtTarget());
        }*/
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

		if (hit.collider != null && hit.collider.CompareTag("Allies") && hit.collider.gameObject != transform.parent.gameObject)
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
		}
	}

	private void Healing()
	{
		if (TargetParent == null) return;
		
            AddBaffEnergyOfSpirit();
            float heal = Heal + ScriptInstanceCount;
            float realHeal = TargetParent.GetComponent<HealthPlayer>().MaxHealth - TargetParent.GetComponent<HealthPlayer>().Health;
            if (realHeal <= heal)
            {
                heal = realHeal;
            }
				if (heal > 0)
				{
					TargetParent.GetComponent<HealthPlayer>().AddHeal(heal);
                    _player.GetComponent<ManaPlayer>().AddMana(heal *0.1f);
                }
            _player.GetComponent<ManaPlayer>().UseMana(1f); // ToDo вынести ману в атрибуты
			FirstAbilityEvent?.Invoke(Heal);
		
    }

	private void AddBaffEnergyOfSpirit()
	{
		if (ScriptInstanceCount >= maxScriptInstances) return;	
			_canCast = true;
			_newPrefab = Instantiate(EnergySpiritEffect);
			_energyOfSpiritPrefab = _newPrefab.GetComponent<EnergyOfSpirit>();
            _energyOfSpiritPrefab.Destroyed += OnScriptInstanceDestroyed;

			_newPrefab.transform.SetParent(TargetParent.transform);
			_newPrefab.GetComponentInChildren<BaffDebaffEffectPrefab>().StartCountdown(_countdownForEnergyOfSpirit);
			ScriptInstanceCount++;
            EnergyOfSpiritBuffs();       
    }

	private void EnergyOfSpiritBuffs()
	{
        switch (ScriptInstanceCount)
        {
			case 0:
                shieldBuff = 0;
                manaBuff = 0;
				break;
            case 1:
                shieldBuff = 10;
				manaBuff = 0.1f;
                break;
            case 2:
                shieldBuff = 15;
				manaBuff = 0.2f;
                break;
            default:
                break;
        }
        // Увеличение прочности накладываемого щита
		transform.GetComponent<TwoRangeProtection>().AddShieldBuff(shieldBuff*0.01f);
    }
	private void OnScriptInstanceDestroyed(EnergyOfSpirit destroyedScript )
	{
		ScriptInstanceDestroyed?.Invoke(destroyedScript);
        ScriptInstanceCount--;
	}

	private IEnumerator Cast()
	{
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
