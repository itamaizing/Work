using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FourRangeRecovery : AbilityBase
{
    [Header("Ability properties")]
    [SerializeField] private GameObject RecoveryBaffPrefab;
    [SerializeField] private GameObject ManaCost;

    [HideInInspector] public GameObject Target;

    public delegate void FourthAbilityHandler(float value);
    public event FourthAbilityHandler FourthAbilityEvent;

    private GameObject _newPrefab;

    protected override KeyCode ActivationKey => KeyCode.Alpha4;

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

        if (Input.GetMouseButtonDown(0) && _player.GetComponent<PlayerMove>().IsSelect && ToggleAbility.gameObject.activeSelf && ToggleAbility.enabled == true)
        {
            HandleLeftMouseButtonToggle();
        }

        if (Input.GetMouseButtonDown(1) && _player.GetComponent<PlayerMove>().IsSelect && ToggleAbility.gameObject.activeSelf && ToggleAbility.enabled == true)
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
        if (AbilityTypeManager.ActiveAbilityType == 1 && _player.GetComponent<PlayerMove>().IsSelect && Abilities.gameObject.activeSelf)
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

        if (hit.collider != null && hit.collider.CompareTag("Allies") && hit.collider.gameObject != gameObject)
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
            _castCoroutine = StartCoroutine(CastProtect(1.2f));
        }
    }

    private void Heal()
    {
        if (TargetParent != null && TargetParent.GetComponent<HealthRecovery>() != null)
        {
            TargetParent.GetComponent<HealthRecovery>().Timer = Time.time;
            _newPrefab.GetComponentInChildren<BaffDebaffEffectPrefab>().StartCountdown(12);
        }
        else if (TargetParent != null)
        {
            _newPrefab = Instantiate(RecoveryBaffPrefab);
            _newPrefab.transform.SetParent(TargetParent.transform);
            _newPrefab.GetComponentInChildren<BaffDebaffEffectPrefab>().StartCountdown(12);
            _newPrefab.GetComponent<HealthRecovery>().CastRecovery(12f, 6f, 3f);


        }
        _player.GetComponent<ManaPlayer>().UseMana(4f);

        FourthAbilityEvent?.Invoke(0f);
        Recharge();
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
        _player.GetComponent<PlayerMove>().CanMove = false;
        CreateCastPrefab(castTime);

        yield return new WaitForSeconds(castTime);
        _castCoroutine = null;
        _player.GetComponent<PlayerMove>().CanMove = true;
        Heal();
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
        return;
    }
}