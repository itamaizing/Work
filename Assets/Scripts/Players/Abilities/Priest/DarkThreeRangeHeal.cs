using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class DarkThreeRangeHeal : AbilityBase
{
    [Header("Ability properties")]
    [SerializeField] private GameObject ManaCost;

    [HideInInspector] public GameObject Target;

    public delegate void DarkThirdAbilityHandler(float value);
    public event DarkThirdAbilityHandler DarkThirdAbilityEvent;

    protected override KeyCode ActivationKey => KeyCode.Alpha3;

    private void Start()
    {
        Distance = 6f * 1.9f;
        AttackType = AttackType.OneAttack;
        AbilityType = AbilityType.DamageAbility;
        AttackRangeType = AttackRangeType.RangeAttack;
    }

    void Update()
    {
        HandleToggleAbility();
        Target = TargetParent;
    }


    protected override void HandleToggleAbility()
    {
        base.HandleToggleAbility();
        // Текущий код в методе Update

        if (Input.GetMouseButtonDown(0) && Abilities.gameObject.activeSelf && ToggleAbility.enabled && _player.GetComponent<MoveComponent>().IsSelect)
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
        return;
    }

    public override void OnLeftDoubleClick()
    {
        if (ShouldUseToggleTarget() || _isInputDoubleClick)
        {
            StartCoroutine(ToggleDoubleClick());
        }

        else if (AbilityTypeManager.ActiveAbilityType == 1 && _player.GetComponent<MoveComponent>().IsSelect && Abilities.gameObject.activeSelf)
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

        if (hit.collider != null && hit.collider.CompareTag("Enemies") && hit.collider.gameObject != gameObject)
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
            _castCoroutine = StartCoroutine(CastProtect(1.8f));
        }
    }

    private void Damage()
    {
        if (TargetParent != null)
        {
            TargetParent.GetComponent<HealthComponent>().TryTakeDamage(35f, DamageType.Magical, AttackRangeType.RangeAttack);
            _player.GetComponent<Mana>().Use(30f);

            DarkThirdAbilityEvent?.Invoke(35f);
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
        _player.GetComponent<MoveComponent>().CanMove = false;
        CreateCastPrefab(castTime);

        yield return new WaitForSeconds(castTime);

        _castCoroutine = null;
        _player.GetComponent<MoveComponent>().CanMove = true;
        Select.GetComponent<SelectObject>().CanSelect = true;

        Damage();
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

