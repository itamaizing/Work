using GlobalEvents;
using Players.Abilities.Genjalf.Shield_Ability;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreeperSingleAttack : AbilityBase
{
    public delegate void FirstAbilityHandler(float value);
    public event FirstAbilityHandler FirstAbilityEvent;

    [HideInInspector] public float DamageRate = 1.2f;
    [HideInInspector] public bool IsGlobalCooldown = true;
    [HideInInspector] public bool TargetCanAvoidance = true;

    protected override KeyCode ActivationKey => KeyCode.Alpha1;

    private void Start()
    {
        Distance = _cellSize * CellDistance;
        AttackType = AttackType.Autoattack;
        AbilityType = AbilityType.DamageAbility;
        AttackRangeType = AttackRangeType.MeleeAttack;
    }

    private void Update()
    {
        HandleToggleAbility();
    }

    protected override void HandleToggleAbility()
    {
        // Текущий код в методе Update
        base.HandleToggleAbility();
        if (Input.GetMouseButtonDown(0) && ToggleAbility.gameObject.activeSelf && ToggleAbility.enabled && _player.GetComponent<MoveComponent>().IsSelect)
        {
            HandleLeftMouseButtonToggle();
        }
    }
    protected override void HandleToggleAbilityOn()
    {
        base.HandleToggleAbilityOn();
        // Включенный ToggleAbility
        
        if (TargetParent == null)
        {
            HandlePrefabVisibility();
            HandleTargetSelection();
        }

        if (TargetParent != null)
        {
            HandleDistanceToTarget();
        }
    }
    protected override void HandleToggleAbilityOff()
    {
        base.HandleToggleAbilityOff();
        // Выключенный ToggleAbility
        StopBackgroundSwitcherEvent.SendStartStopBackgroundSwitcher();
        TargetParent = null;
        CanDealDamageOrHeal = false;
        CanMakeDamage = false;
    }
    public override void ChangeBoolAndValues()
    {
        _targetHealth = TargetParent.GetComponent<HealthComponent>();
        CanMakeDamage = true;
        CanDealDamageOrHeal = true;
        Destroy(NewAbilityPrefab);
    }

    public override void OnLeftDoubleClick()
    {
        if (ShouldUseToggleTarget() || _isInputDoubleClick)
        {
            StartCoroutine(ToggleDoubleClick());
        }
        else if (AbilityTypeManager.ActiveAbilityType == 1 && _player.GetComponent<MoveComponent>().IsSelect &&
                 Abilities.gameObject.activeSelf)
        {
            StartCoroutine(DoNotDoubleClickAtTarget());
        }
    }

    public override void OnRightDoubleClick()
    {
    }

    private void HandleTargetSelection()
    {
        // Выбор врага
        _targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(_targetPosition, Vector2.zero);
        if (hit.collider != null && hit.collider.CompareTag("Enemies") && hit.collider.gameObject != gameObject)
        {
            TargetParent = hit.collider.gameObject;
            ChangeBoolAndValues();

            if (NewAbilityPrefab != null)
            {
                Destroy(NewAbilityPrefab);
            }
        }
    }

    public override void HandleDealDamageOrHeal()
    {
        // Нанесение урона
        _damageValue = Random.Range(7, 12);

        if (CanMakeDamage && _castCoroutine == null && CanUseAbility)
        {
            if (Abilities.GetComponent<GlobalCooldown>() && IsGlobalCooldown)
            {
                Abilities.GetComponent<GlobalCooldown>().StartGlobalCooldown();
            }

            FirstAbilityEvent?.Invoke(_damageValue);
            _castCoroutine = StartCoroutine(Damage(DamageRate));
        }
    }

    private IEnumerator Damage(float damageRate)
    {
        if (TargetCanAvoidance)
        {
            MissAtDistance();
        }

        yield return new WaitForSeconds(damageRate / 2);

        Shield _shield = null;

        if (TargetParent != null)
        {
            _shield = TargetParent.GetComponentInChildren<Shield>();
        }

        if (_shield != null)
        {
            _shield.DamageInShield(_damageValue);
            CanMakeDamage = false;

            yield return new WaitForSeconds(damageRate / 2);

            _castCoroutine = null;
            CanMakeDamage = true;
        }
        else
        {
            _targetHealth.TryTakeDamage(_damageValue, DamageType.Physical, AttackRangeType.MeleeAttack);
            CanMakeDamage = false;

            yield return new WaitForSeconds(damageRate / 2);

            _castCoroutine = null;
            CanMakeDamage = true;
        }
    }

    private void MissAtDistance()
    {
        // Промах при отдаление более чем на 10% от корпуса
        if (previousPosition == Vector3.zero)
        {
            previousPosition = TargetParent.transform.position;
        }

        float time = 0;
        time += Time.deltaTime;
        if (time < 0.6f)
        {
            Vector3 currentPosition = TargetParent.transform.position;

            if (Vector3.Distance(previousPosition, currentPosition) >= 0.19f)
            {
                _damageValue = 0;
            }

            previousPosition = currentPosition;
        }
    }
}
