using System.Collections;
using UnityEngine;

public class OneRangeAttack : AbilityBase
{
	// "Искра Света" - Лечение и бафф союзника , урон по врагу. 
	[HideInInspector] public float Heal = 2f;
	[HideInInspector] public int SpiritDebaffCount = 0;
    [HideInInspector] public int SpiritBaffCount = 0;
    [HideInInspector] public GameObject Target;
    [SerializeField] private GameObject _manaCost;

    [Header("Light Ability properties")]
	[SerializeField] private GameObject _baffEffect;
	[SerializeField] private float _lightCastTime;
    [SerializeField] private float _countdownSpiritBaff;
    [SerializeField] private int _maxBaffCount = 2;
    [SerializeField] private float _heal;
    [SerializeField] private float _manaForHeal;
    [Header("Dark Ability properties")]
    [SerializeField] private GameObject _debaffEffect;
    [SerializeField] private float _darkCastTime;
    [SerializeField] private float _countdownSpiritDebaff;
    [SerializeField] private float _damage;
    [SerializeField] private int _maxDebaffCount = 2;
    [SerializeField] private float _manaForDamage;

    private GameObject _baffPrefab;
	private EnergyOfSpirit _baffEffectPrefab;
    private GameObject _debaffPrefab;
    private HealthOfSpirit _debaffEffectPrefab;
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
	private float healBuff;
	public float HealBuff
	{
		get { return healBuff;}
		set { healBuff = value; }
	}

	public bool isLightSide = true;

    public delegate void FirstAbilityHandler(float value);
    public event FirstAbilityHandler FirstAbilityEvent;
    public event System.Action<EnergyOfSpirit> BaffPrefabDestroyed;
	
    public delegate void DarkFirstAbilityHandler(float value);
    public event DarkFirstAbilityHandler DarkFirstAbilityEvent;
    public event System.Action<HealthOfSpirit> DebaffPrefabDestroyed;

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

		if (Input.GetMouseButtonDown(0) && _player.GetComponent<MoveComponent>().IsSelect && ToggleAbility.gameObject.activeSelf)
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
			if (_manaCost != null)
			{
				_manaCost.SetActive(true);
				_manaCost.GetComponent<VisualManaCost>().CheckManaCost();
				_manaCost.transform.localScale = new Vector2(0.1f, _manaCost.gameObject.transform.localScale.y);
			}

			HandlePrefabVisibility();
			HandleTargetSelection();
		}

		if (TargetParent != null)
		{
			if (_manaCost != null)
			{
				_manaCost.gameObject.SetActive(false);
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
			_manaCost.gameObject.SetActive(false);
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
        else if (AbilityTypeManager.ActiveAbilityType == 1 && _player.GetComponent<MoveComponent>().IsSelect && Abilities.gameObject.activeSelf)
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

		if (isLightSide && hit.collider != null && hit.collider.CompareTag("Allies") && hit.collider.gameObject != transform.parent.gameObject)
		{
			TargetParent = hit.collider.gameObject;

			CanDealDamageOrHeal = true;
			_canCast = true;

			if (NewAbilityPrefab != null)
			{
				Destroy(NewAbilityPrefab);
			}
		}

        if (!isLightSide && hit.collider != null && hit.collider.CompareTag("Enemies"))
        {
            TargetParent = hit.collider.gameObject;

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
			if (isLightSide)
			{
				_castCoroutine = StartCoroutine(Cast(_lightCastTime,true));
				CreateCastPrefab(_lightCastTime);
			}
			else
			{
                _castCoroutine = StartCoroutine(Cast(_darkCastTime,false));
                CreateCastPrefab(_darkCastTime);
            }
		}
	}
    private void ResetAllEffects(EffectsType type)
    {
        EnergyOfSpirit[] baffs = TargetParent.GetComponentsInChildren<EnergyOfSpirit>();
		HealthOfSpirit[] debaffs = TargetParent.GetComponentsInChildren<HealthOfSpirit>();
        BaffDebaffEffectPrefab[] baffEffects = TargetParent.GetComponentsInChildren<BaffDebaffEffectPrefab>();

        foreach (var baff in baffs)
        {
            baff.StartCountdownCoroutine(_countdownSpiritBaff);
        }
		foreach(var debaff in debaffs)
		{
			debaff.StartCountdownCoroutine(_countdownSpiritDebaff);
		}
        foreach (var baff in baffEffects)
        {
			if(baff.BaffType==type)
            baff.StartCountdown(_countdownSpiritDebaff);
        }
    }
	public void ReverseAbility(bool isLight)
	{
		ToggleAbility.isOn = false;
        isLightSide = isLight;
		_castCoroutine= null;
		if(isLightSide)
		{
            AbilityType = AbilityType.HealAbility;
        }
		else
		{
            AbilityType = AbilityType.DamageAbility;
        }
	}

    private void Healing()
	{
		if (TargetParent == null) return;
		
            AddBaffEnergyOfSpirit();
            float heal = Heal + SpiritBaffCount;
            /*float realHeal = TargetParent.GetComponent<HealthComponent>()._maxHealth - TargetParent.GetComponent<HealthComponent>()._currentHealth;
            if (realHeal <= heal)
            {
                heal = realHeal;
            }
			if (heal > 0)
			{
			TargetParent.GetComponent<HealthComponent>().AddHeal(heal);
            _player.GetComponent<Mana>().Add(heal *0.1f);
            }
            _player.GetComponent<Mana>().Use(_manaForHeal);
			*/
			FirstAbilityEvent?.Invoke(heal);
		
    }
	private void AddBaffEnergyOfSpirit()
	{
		Debug.Log(SpiritBaffCount);
		if (SpiritBaffCount >= _maxBaffCount)
		{
			ResetAllEffects(EffectsType.EnergyOfSpirit);
			return;
		}
		_canCast = true;
		_baffPrefab = Instantiate(_baffEffect);
		_baffPrefab.transform.SetParent(TargetParent.transform);
		_baffEffectPrefab = _baffPrefab.GetComponent<EnergyOfSpirit>();
		_baffEffectPrefab.Destroyed += OnBaffPrefabDestroyed;
		EnergyOfSpiritBaffs();
	}
	private void EnergyOfSpiritBaffs()
	{
        SpiritBaffCount++;
        switch (SpiritBaffCount)
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
		ResetAllEffects(EffectsType.EnergyOfSpirit);
    }

	private void Damage()
	{
        if (TargetParent == null) return;
        AddDebaff();
        TargetParent.GetComponent<HealthComponent>().TryTakeDamage(_damage + SpiritDebaffCount, DamageType.Magical, AttackRangeType.RangeAttack);
        _player.GetComponent<Mana>().Use(_manaForDamage);
        DarkFirstAbilityEvent?.Invoke(_damage);
    }

	private void AddDebaff()
	{
		Debug.Log(SpiritDebaffCount);
        if (SpiritDebaffCount >= _maxDebaffCount)
        {
            ResetAllEffects(EffectsType.HealthOfSpirit);
            return;
        }
        _canCast = true;
        _debaffPrefab = Instantiate(_debaffEffect);
        _debaffPrefab.transform.SetParent(TargetParent.transform);
        _debaffEffectPrefab = _debaffPrefab.GetComponent<HealthOfSpirit>();
        _debaffEffectPrefab.Destroyed += OnDebaffPrefabDestroyed;
        Debaffs();
    }

	private void Debaffs()
	{
		SpiritDebaffCount++;
        switch (SpiritBaffCount)
        {
            case 0:
                healBuff = 0;
                break;
            case 1:
                healBuff = 0.1f;
                break;
            case 2:
                healBuff = 0.15f;
                break;
            default:
                break;
        }
        ResetAllEffects(EffectsType.HealthOfSpirit);

    }
	private IEnumerator Cast(float time,bool isLight)
	{
        if (Abilities.activeSelf && Abilities.GetComponent<GlobalCooldown>())
		{
			Abilities.GetComponent<GlobalCooldown>().StartGlobalCooldown();
		}
		StartCoroutine(CastMove());
        yield return new WaitForSeconds(time);
		if(isLight)
		{
            Healing();
        }
		else
		{
			Damage();
		}
		lastAbility.AddLastAbility(this);
        this.transform.root.GetComponentInChildren<FourRangeRecovery>().canCast = true;
		_castCoroutine = null;
		yield break;
	}

	private IEnumerator CastMove()
	{
		GetComponentInParent<MoveComponent>().CanMove = false;
		yield return new WaitForSeconds(0.2f);
		GetComponentInParent<MoveComponent>().CanMove = true;

	}


    private void OnBaffPrefabDestroyed(EnergyOfSpirit destroyedScript)
    {
        BaffPrefabDestroyed?.Invoke(destroyedScript);
        SpiritBaffCount--;
    }
    private void OnDebaffPrefabDestroyed(HealthOfSpirit destroyedScript)
    {
        DebaffPrefabDestroyed?.Invoke(destroyedScript);
        SpiritDebaffCount--;
    }
}
