using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class TwoMeleeAttack : AbilityBase
{
    [Header("Ability properties")]
    [HideInInspector] public GameObject Target;

    public delegate void SecondAbilityHandler(float value);
    public event SecondAbilityHandler SecondAbilityEvent;

    private bool _isOneChange;
    private float _chanceCriticalAttack = 0.05f;
    private float _bleedChance = 0.15f;
    private Toggle _toggleFirstAbility;
    private bool _isDamageCooldownRunning = false;
    private float _darts;
    private GameObject _talents;

    protected override KeyCode ActivationKey => KeyCode.Alpha2;

    private void Start()
    {
        Distance = 1.9f * 1.8f;
        AttackType = AttackType.Autoattack;
        AbilityType = AbilityType.DamageAbility;
        AttackRangeType = AttackRangeType.MeleeAttack;
    }

    private void Update()
    {
        Target = TargetParent;
        if (_player != null)
        {
            _talents = _player.transform.Find("Talents").gameObject;
            _darts = _talents.GetComponent<DeepWounds>().Darts;
        }

        if (_toggleFirstAbility == null && _playerAbility != null)
        {
            _toggleFirstAbility = _playerAbility.GetComponent<OneMeleeAttack>().ToggleAbility;

        }
        HandleToggleAbility();
    }

    protected override void HandleToggleAbility()
    {
        base.HandleToggleAbility();

        // Текущий код в методе Update
        if (_toggleFirstAbility != null && !_toggleFirstAbility.isOn && !ToggleAbility.isOn)
        {
            TargetParent = null;
            _isOneChange = false;
        }
        if (_toggleFirstAbility != null && _toggleFirstAbility.isOn)
        {
            _isOneChange = false;
        }

        if (Input.GetMouseButtonDown(0) && _player.GetComponent<PlayerMove>().IsSelect && Abilities.gameObject.activeSelf && ToggleAbility.enabled)
        {
            HandleLeftMouseButtonToggle();
        }

        if (Input.GetMouseButtonDown(1) && _player.GetComponent<PlayerMove>().IsSelect && Abilities.gameObject.activeSelf)
        {
            HandleRightMouseButtonToggle();

            if (AbilityTypeManager.ActiveAbilityType == 1 && _playerAbility.GetComponent<FourMeleeAttack>().ToggleAbility.isOn == false && ToggleAbility.enabled)
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

        if (_playerAbility.GetComponent<OneMeleeAttack>().TargetParent != null)
        {
            TargetParent = _playerAbility.GetComponent<OneMeleeAttack>().TargetParent;
            
            if (_isOneChange == false)
            {
                ChangeBoolAndValues();
            }
        }

        if(_darts > 0)
        {
            Distance = 1.94f * 6;
        }
        else
        {
            Distance = 1.9f * 1.8f;
        }

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
        // Выключенный ToggleAbility
        base.HandleToggleAbilityOff();

        CanDealDamageOrHeal = false;
        CanMakeDamage = false;

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
        _targetHealth = TargetParent.GetComponent<HealthPlayer>();
        CanMakeDamage = true;
        CanDealDamageOrHeal = true;
        _isOneChange = true;
        Destroy(NewAbilityPrefab);
    }

    private void HandleTargetSelection()
    {
        // Выбор врага
        _targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(_targetPosition, Vector2.zero);

        if (hit.collider != null && hit.collider.CompareTag("Enemies") && hit.collider.gameObject != gameObject && hit.collider.GetComponent<Uterus>() == null)
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
        _damageValue = Random.Range(11, 14);

        if (CanMakeDamage && _castCoroutine == null && CanUseAbility)
        {
            if (Abilities.GetComponent<GlobalCooldown>())
            {
                Abilities.GetComponent<GlobalCooldown>().StartGlobalCooldown();
            }

            if (Random.value < _chanceCriticalAttack)
            {
                _damageValue *= 1.6f;
            }

            if(_darts > 0)
            {
                _damageValue *= 0.7f;
                _talents.GetComponent<DeepWounds>().UseDarts();
                _talents.GetComponent<PoisonTalent>().ApplyPoison(this);
            }
            else
            {
                SecondAbilityEvent?.Invoke(_damageValue);
            }

            _targetHealth.TakePhisicDamage(_damageValue);
            _player.GetComponent<PsionicaMelee>().MakePsionica(_damageValue);

            float activePsionica = _playerAbility.GetComponent<FiveConversion>().PsionicaActive;

            if (activePsionica > 0)
            {
                HandleActivePsionica(_damageValue, activePsionica);
            }

            if (Random.value <= _bleedChance && _playerAbility != null)
            {
               _playerAbility.GetComponent<Bleeding>().StartBleed(Target, this);
            }
            _castCoroutine = StartCoroutine(DamageCooldown());
        }
    }

    private void HandleActivePsionica(float damageValue, float activePsionica)
    {
        StartCoroutine(DamageEnemyCooldown(activePsionica));

        HandleEffectsOnTarget(activePsionica, TargetParent);
        HandleEffectsOnNearbyEnemies(activePsionica, damageValue);

        GetComponent<FiveConversion>().UseActivePsionica(activePsionica,Target);
    }

    private void HandleEffectsOnTarget(float activePsionica, GameObject target)
    {
        // Обработка эффектов на основной цели
        if (activePsionica >= 10 && activePsionica < 20)
        {
            List<BaseEffect> buffEffects = new List<BaseEffect>();
            Component[] allEffects = target.GetComponents<Component>();

            foreach (Component effectComponent in allEffects)
            {
                if (effectComponent is BaseEffect effect && effect.Type == EffectType.Buff)
                {
                    buffEffects.Add(effect);
                }
            }

            if (buffEffects.Count > 0)
            {
                for (int i = 0; i < 1; i++)
                {
                    Destroy(buffEffects[i]);
                }
            }
        }

        // Перемещение к цели, если активная псионика больше или равна 30
        if (activePsionica >= 30)
        {
            MoveTowardsEnemy(target);
        }
    }

    private void HandleEffectsOnNearbyEnemies(float activePsionica, float damageValue)
    {
        // Обработка эффектов на других врагах в радиусе

        Collider2D[] colliders = Physics2D.OverlapCircleAll(_player.transform.position, radius);

        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag("Enemies") && collider.gameObject != gameObject && collider.gameObject != TargetParent)
            {
                StartCoroutine(DamageEnemiesCooldown(activePsionica, collider));
                // Обработка эффектов на других врагах
                HandleEffectsOnTarget(activePsionica, collider.gameObject);

                // Перемещение к врагу, если активная псионика больше или равна 30
                if (activePsionica >= 30)
                {
                    MoveTowardsEnemy(collider.gameObject);
                }
            }
        }
    }
    void MoveTowardsEnemy(GameObject Target)
    {
        float distanceFromPlayer = 1.94f;
        float moveSpeed = 15f;

        Vector3 directionToPlayer = _player.transform.position - Target.transform.position;
        Vector3 normalizedDirection = directionToPlayer.normalized;
        Vector3 targetPosition = Target.transform.position - normalizedDirection * distanceFromPlayer;

        StartCoroutine(MoveTowardsCoroutine(Target, targetPosition, moveSpeed));
    }

    private IEnumerator DamageCooldown()
    {
        if (_isDamageCooldownRunning)
        {
            yield break; // Если корутина уже выполняется, просто выходим
        }

        _isDamageCooldownRunning = true;
        CanMakeDamage = false;

        yield return new WaitForSeconds(1.4f);

        CanMakeDamage = true;
        _castCoroutine = null;
        _isDamageCooldownRunning = false;
    }

    private IEnumerator DamageEnemyCooldown(float activePsionica)
    {
        yield return new WaitForSeconds(0.1f);
        // Нанесение урона основной цели
        _targetHealth.TakeMagicDamage(activePsionica * 0.3f);

    }
    private IEnumerator DamageEnemiesCooldown(float activePsionica, Collider2D collider)
    {
        yield return new WaitForSeconds(0.1f);
        // Нанесение урона врагам в радиусе
        collider.GetComponent<HealthPlayer>().TakeMagicDamage(activePsionica * 0.5f);
    }

    private IEnumerator MoveTowardsCoroutine(GameObject target, Vector3 targetPosition, float moveSpeed)
    {
        Rigidbody2D rb = target.GetComponent<Rigidbody2D>();
        float startTime = Time.time;

        while (Time.time - startTime < 0.5f)
        {
            rb.isKinematic = false;
            rb.MovePosition(Vector2.MoveTowards(target.transform.position, targetPosition, moveSpeed * Time.fixedDeltaTime));
            yield return new WaitForFixedUpdate();
        }

        rb.isKinematic = true; 
    }
}