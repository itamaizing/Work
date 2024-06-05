using System.Collections;
using TMPro;
using UnityEngine;


public class TwoRangeProtection : AbilityBase
{ 
    [Header("Ability properties")]
    [SerializeField] private GameObject ProtectBaff;
    [SerializeField] private GameObject ProtectDebaff;
    [SerializeField] private GameObject CooldownButton;
    [SerializeField] private GameObject ManaCost;

    [SerializeField] private GameObject DarkProtectDebaff;

    public delegate void SecondAbilityHandler(float value);
    public event SecondAbilityHandler SecondAbilityEvent;

    public delegate void SecondDarkAbilityHandler(float value);
    public event SecondDarkAbilityHandler SecondDarkAbilityEvent;

    private GameObject _protectBaffPrefab;
    private GameObject _protectDebaffPrefab;
    private GameObject _darkProtectDebaff;

    private float _absorbtionBuff;

    private bool isLightSide=true;

    protected override KeyCode ActivationKey => KeyCode.Alpha2;

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
        base.HandleToggleAbility();
        // Текущий код в методе Update

        if (Input.GetMouseButtonDown(0) && _player.GetComponent<MoveComponent>().IsSelect && ToggleAbility.gameObject.activeSelf)
        {
            HandleLeftMouseButtonToggle();
        }

        if (Input.GetMouseButtonDown(1) && _player.GetComponent<MoveComponent>().IsSelect && ToggleAbility.gameObject.activeSelf)
        {
            HandleRightMouseButtonToggle();

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
                ManaCost.transform.localScale = new Vector2(0.6f, ManaCost.gameObject.transform.localScale.y);
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
        return;
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
        Destroy(NewAbilityPrefab);
    }

    private void HandleTargetSelection()
    {
        // Выбор врага
        _targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(_targetPosition, Vector2.zero);

        if (isLightSide && hit.collider != null && hit.collider.CompareTag("Allies") && hit.collider.gameObject != gameObject &&
                hit.collider.gameObject.GetComponentInChildren<DebaffProtect>() == null)
        {
            TargetParent = hit.collider.gameObject;

            if (NewAbilityPrefab != null)
            {
                Destroy(NewAbilityPrefab);
            }
            DrawCircle.Clear();
        }
        else if (isLightSide && hit.collider != null && hit.collider.CompareTag("Allies") && hit.collider.gameObject == transform.parent.gameObject)
        {
            TargetParent = transform.parent.gameObject;
            if (NewAbilityPrefab != null)
            {
                Destroy(NewAbilityPrefab);
            }
            DrawCircle.Clear();
        }
        else if (!isLightSide && hit.collider != null && hit.collider.CompareTag("Enemies") 
              && hit.collider.gameObject != gameObject
              && hit.collider.GetComponent<DebaffRepeatedDamage>()==null)
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
        if (_castCoroutine != null) return;
       
            if (TargetParent == transform.parent.gameObject&&isLightSide)
            {
                _castCoroutine = StartCoroutine(CastCoroutine(0f));
            }
            else if(TargetParent!=transform.parent.gameObject&&isLightSide)
            {
                _castCoroutine = StartCoroutine(CastCoroutine(1.2f));
            }
            else if(TargetParent!=transform.parent.gameObject&&!isLightSide)
        {
                 _castCoroutine = StartCoroutine(CastCoroutine(1.2f));
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

    private void Cast()
    {
        if(isLightSide)
        {
            _player.GetComponent<Mana>().Use(6f);
            SetProtectBuff();
            SetProtectDebaff();
            SecondAbilityEvent?.Invoke(0f);
        }
        else
        {
            _player.GetComponent<Mana>().Use(20f);
            SetDarkProtectionDebaff();
            SetProtectDebaff();
            SecondDarkAbilityEvent?.Invoke(0f);
        }
            StartCoroutine(Recharge());
    }

    private void SetProtectBuff()
    {
        DamageAbsorption shield = TargetParent.GetComponentInChildren<DamageAbsorption>();
        if (shield!=null) Destroy(shield);
            _protectBaffPrefab = Instantiate(ProtectBaff);
            _protectBaffPrefab.transform.SetParent(TargetParent.transform);
            _protectBaffPrefab.GetComponent<DamageAbsorption>().AddBuffAbsorbtion(_absorbtionBuff);
            float absorbtionTime = _protectBaffPrefab.GetComponent<DamageAbsorption>().Duration;
            _protectBaffPrefab.GetComponentInChildren<BaffDebaffEffectPrefab>().StartCountdown(absorbtionTime);
    }
    private void SetProtectDebaff()
    {
            _protectDebaffPrefab = Instantiate(ProtectDebaff);
            _protectDebaffPrefab.transform.SetParent(TargetParent.transform);
            _protectDebaffPrefab.GetComponent<DebaffProtect>().CastDebaff(12f);
            _protectDebaffPrefab.GetComponentInChildren<BaffDebaffEffectPrefab>().StartCountdown(12);
    }

    private void SetDarkProtectionDebaff()
    {
        _darkProtectDebaff = Instantiate(DarkProtectDebaff);
        _darkProtectDebaff.transform.SetParent(TargetParent.transform);
        _darkProtectDebaff.AddComponent<DebaffRepeatedDamage>();
        _darkProtectDebaff.GetComponent<DebaffRepeatedDamage>().CastDebaff(8f) ;
        _darkProtectDebaff.GetComponentInChildren<BaffDebaffEffectPrefab>().StartCountdown(12);

        GetComponent<FiveReversPolarity>().UseReversePolarity(0f);
    }

    public void AddShieldBuff(float value)
    {
        _absorbtionBuff = value;
    }

    private IEnumerator CastCoroutine(float castTime)
    { 
        if (Abilities.GetComponent<GlobalCooldown>())
        {
            Abilities.GetComponent<GlobalCooldown>().StartGlobalCooldown();
        }

        ToggleAbility.enabled = false;
        _player.GetComponent<MoveComponent>().CanMove = false;
        CreateCastPrefab(castTime);

        yield return new WaitForSeconds(castTime);

        ToggleAbility.enabled = true;
        _player.GetComponent<MoveComponent>().CanMove = true;
        _castCoroutine = null;

		this.transform.root.GetComponentInChildren<FourRangeRecovery>().canCast = true;

		Cast();
        lastAbility.AddLastAbility(this);
    }


    private IEnumerator Recharge()
    {
        ToggleAbility.isOn = false;
        ToggleAbility.enabled = false;
        _playerAbility.GetComponent<TwoRangeProtection>().enabled = false;

        TargetParent = null;
        _absorbtionBuff = 0;

        CooldownButton.gameObject.SetActive(true);
        StartCoroutine(CountdownRoutine(4));

        yield return new WaitForSeconds(4f);

        CooldownButton.gameObject.SetActive(false);
        ToggleAbility.enabled = true;
        _playerAbility.GetComponent<TwoRangeProtection>().enabled = true;
        yield break;

    }

    public IEnumerator CountdownRoutine(int time)
    {
        CooldownButton.GetComponent<ClickButtonCooldown>().TimeCooldown = time;

        while (time > 0)
        {
            CooldownButton.GetComponentInChildren<TextMeshPro>().text = time.ToString();
            yield return new WaitForSeconds(1f);
            time--;
        }
    }
}