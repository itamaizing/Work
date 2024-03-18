using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class DarkTwoRangeProtection : AbilityBase
{
    [Header("Ability properties")]
    [SerializeField] private GameObject DarkProtectDebaff;
    [SerializeField] private GameObject CooldownButton;
    [SerializeField] private GameObject ManaCost;

    [HideInInspector] public GameObject Target;

    public delegate void SecondDarkAbilityHandler(float value);
    public event SecondDarkAbilityHandler SecondDarkAbilityEvent;

    private GameObject _newPrefab;

    protected override KeyCode ActivationKey => KeyCode.Alpha2;

    private void Start()
    {
        Distance = 6f * 1.9f;
        AttackType = AttackType.OneAttack;
        AbilityType = AbilityType.DamageAbility;
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

        if (Input.GetMouseButtonDown(0) && _player.GetComponent<PlayerMove>().IsSelect && Abilities.gameObject.activeSelf)
        {
            HandleLeftMouseButtonToggle();
        }

        if (Input.GetMouseButtonDown(1) && _player.GetComponent<PlayerMove>().IsSelect && Abilities.gameObject.activeSelf)
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

        if (hit.collider != null && hit.collider.CompareTag("Enemies") && hit.collider.gameObject != gameObject
            && !hit.collider.GetComponent<DebaffRepeatedDamage>())
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
            _castCoroutine = StartCoroutine(CastProtect(1.2f));
        }
    }

    private void Protect()
    {
        if (TargetParent != null)
        {
            GetComponent<ManaPlayer>().UseMana(20f);

            TargetParent.AddComponent<DebaffRepeatedDamage>();
            TargetParent.GetComponent<DebaffRepeatedDamage>().CastDebaff(8f);


            if (_newPrefab != null)
            {
                Destroy(_newPrefab);
            }

            _newPrefab = Instantiate(DarkProtectDebaff);
            _newPrefab.transform.SetParent(TargetParent.transform);
            _newPrefab.GetComponentInChildren<BaffDebaffEffectPrefab>().StartCountdown(12);

            SecondDarkAbilityEvent?.Invoke(0f);
        }
        StartCoroutine(Recharge());
    }

    private IEnumerator CastProtect(float castTime)
    {
        if (Abilities.GetComponent<GlobalCooldown>())
        {
            Abilities.GetComponent<GlobalCooldown>().StartGlobalCooldown();
        }

        ToggleAbility.enabled = false;
        _player.GetComponent<PlayerMove>().CanMove = false;
        CreateCastPrefab(castTime);

        yield return new WaitForSeconds(castTime);

        ToggleAbility.enabled = true;
        _player.GetComponent<PlayerMove>().CanMove = true;
        Select.GetComponent<SelectObject>().CanSelect = true;
        _castCoroutine = null;

        Protect();
    }


    private IEnumerator Recharge()
    {

        ToggleAbility.isOn = false;
        ToggleAbility.enabled = false;
        GetComponent<TwoRangeProtection>().enabled = false;
        TargetParent = null;

        CooldownButton.gameObject.SetActive(true);
        StartCoroutine(CountdownRoutine(4));

        yield return new WaitForSeconds(4f);

        CooldownButton.gameObject.SetActive(false);
        ToggleAbility.enabled = true;
        GetComponent<TwoRangeProtection>().enabled = true;
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