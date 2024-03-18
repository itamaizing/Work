using System.Collections;
using TMPro;
using UnityEngine;


public class TwoRangeProtection : AbilityBase
{
    [Header("Ability properties")]
    [SerializeField] private GameObject ProtectBaff;
    [SerializeField] private GameObject CooldownButton;
    [SerializeField] private GameObject ManaCost;

    [HideInInspector] public GameObject Target;

    public delegate void SecondAbilityHandler(float value);
    public event SecondAbilityHandler SecondAbilityEvent;

    private GameObject _newPrefab;

    protected override KeyCode ActivationKey => KeyCode.Alpha2;

    private void Start()
    {
        Distance = 6f * 1.9f;
        AttackType = AttackType.OneAttack;
        AbilityType = AbilityType.HealAbility;
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

        if (Input.GetMouseButtonDown(0) && _player.GetComponent<PlayerMove>().IsSelect && ToggleAbility.gameObject.activeSelf)
        {
            HandleLeftMouseButtonToggle();
        }

        if (Input.GetMouseButtonDown(1) && _player.GetComponent<PlayerMove>().IsSelect && ToggleAbility.gameObject.activeSelf)
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

        if (hit.collider != null && hit.collider.CompareTag("Allies") && hit.collider.gameObject != gameObject &&
                hit.collider.gameObject.GetComponent<DebaffProtect>() == false)
        {
            TargetParent = hit.collider.gameObject;

            if (NewAbilityPrefab != null)
            {
                Destroy(NewAbilityPrefab);
            }
            DrawCircle.Clear();
        }
        else if (hit.collider != null && hit.collider.CompareTag("Allies") && hit.collider.gameObject == gameObject)
        {
            TargetParent = gameObject;
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
            if (TargetParent == gameObject)
            {
                _castCoroutine = StartCoroutine(CastProtect(0.6f));
            }
            else
            {
                _castCoroutine = StartCoroutine(CastProtect(1.2f));
            }
        }
    }

    private void Protect()
    {
        if (TargetParent != null)
        {
            _player.GetComponent<ManaPlayer>().UseMana(6f);

            TargetParent.AddComponent<DebaffProtect>();
            TargetParent.GetComponent<DebaffProtect>().CastDebaff(8f);


            if (_newPrefab != null)
            {
                Destroy(_newPrefab);
            }

            _newPrefab = Instantiate(ProtectBaff);
            _newPrefab.transform.SetParent(TargetParent.transform);
            _newPrefab.GetComponentInChildren<BaffDebaffEffectPrefab>().StartCountdown(12);

            SecondAbilityEvent?.Invoke(0f);
            StartCoroutine(Recharge());
        }
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
        _castCoroutine = null;

        Protect();
    }


    private IEnumerator Recharge()
    {

        ToggleAbility.isOn = false;
        ToggleAbility.enabled = false;
        _playerAbility.GetComponent<DarkTwoRangeProtection>().enabled = false;
        TargetParent = null;

        CooldownButton.gameObject.SetActive(true);
        StartCoroutine(CountdownRoutine(4));

        yield return new WaitForSeconds(4f);

        CooldownButton.gameObject.SetActive(false);
        ToggleAbility.enabled = true;
        _playerAbility.GetComponent<DarkTwoRangeProtection>().enabled = true;
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