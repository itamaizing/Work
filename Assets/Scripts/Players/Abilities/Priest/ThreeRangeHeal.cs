using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ThreeRangeHeal : AbilityBase
{
    [Header("Ability properties")]
    [SerializeField] private GameObject ManaCost;
    [SerializeField] private float _castTime = 1.8f;
    [SerializeField] private float _darkCastTime = 1.8f;
    [SerializeField] private float _heal = 35f;
    [SerializeField] private float _damage = 35f;
    [SerializeField] private float _manaForCast = 30f;
    [SerializeField] private float _manaForDarkCast = 30f;
     
    public delegate void ThirdAbilityHandler(float value);
    public event ThirdAbilityHandler ThirdAbilityEvent;

    public delegate void DarkThirdAbilityHandler(float value);
    public event DarkThirdAbilityHandler DarkThirdAbilityEvent;

    protected override KeyCode ActivationKey => KeyCode.Alpha3;

    private bool isLightSide = true;

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

        if (Input.GetMouseButtonDown(0) && ToggleAbility.gameObject.activeSelf && ToggleAbility.enabled && _player.GetComponent<PlayerMove>().IsSelect)
        {
            HandleLeftMouseButtonToggle();
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
                ManaCost.transform.localScale = new Vector2(3f, ManaCost.gameObject.transform.localScale.y);
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
    }

    public override void OnLeftDoubleClick()
    {
        if (ShouldUseToggleTarget() || _isInputDoubleClick)
        {
            StartCoroutine(ToggleDoubleClick());
        }

        else if (AbilityTypeManager.ActiveAbilityType == 1 && _player.GetComponent<PlayerMove>().IsSelect && Abilities.gameObject.activeSelf)
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
            TargetParent = gameObject;

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
        if(_castCoroutine == null)
        {
            if(isLightSide)
            _castCoroutine = StartCoroutine(CastProtect(_castTime));
            else
            {
            _castCoroutine = StartCoroutine(CastProtect(_darkCastTime));
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
        float heal = _heal;
        if (TargetParent != null)
        {
            float realHeal = TargetParent.GetComponent<HealthPlayer>().MaxHealth - TargetParent.GetComponent<HealthPlayer>().Health;
            float eneryOfSpiritStacks = _player.GetComponentInChildren<OneRangeAttack>().SpiritBaffCount;
            heal = heal + eneryOfSpiritStacks;
            if (realHeal <= heal)
            {
                heal = realHeal;
            }
            TargetParent.GetComponent<HealthPlayer>().AddHeal(heal);
            if (eneryOfSpiritStacks > 0)
            {
                heal = heal * _player.GetComponentInChildren<OneRangeAttack>().ManaBaff;

                if (heal > 0)
                {
                    _player.GetComponent<ManaPlayer>().AddMana(heal);
                }
            }
        }
        _player.GetComponent<ManaPlayer>().UseMana(_manaForCast);


        ThirdAbilityEvent?.Invoke(heal);
        Recharge();
    }

    private void Damage()
    {
        if (TargetParent != null)
        {
            float eneryOfSpiritStacks = _player.GetComponentInChildren<OneRangeAttack>().SpiritDebaffCount;
            float damage = _damage + eneryOfSpiritStacks;
            TargetParent.GetComponent<HealthPlayer>().TakeMagicDamage(damage);
            _player.GetComponent<ManaPlayer>().UseMana(_manaForDarkCast);

            DarkThirdAbilityEvent?.Invoke(damage);
            Recharge();
        }
    }

    private IEnumerator CastProtect(float castTime)
    {
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
        _player.GetComponent<PlayerMove>().CanMove = false;
        CreateCastPrefab(castTime);

        yield return new WaitForSeconds(castTime);

        _castCoroutine = null;
        _player.GetComponent<PlayerMove>().CanMove = true;
        Select.GetComponent<SelectObject>().CanSelect = true;

		this.transform.root.GetComponentInChildren<FourRangeRecovery>().canCast = true;
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
        ToggleAbility.isOn = false;
        TargetParent = null;
        return;
    }
}
