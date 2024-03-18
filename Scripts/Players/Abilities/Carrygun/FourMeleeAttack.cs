using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FourMeleeAttack : AbilityBase
{
    [Header("Ability properties")]
    [SerializeField] private GameObject CooldownButton;
    [SerializeField] private GameObject HealthAbsorptionDebaff;
    [SerializeField] private GameObject CircleDistancePrefab;
    [SerializeField] private RaycastHit2D[] AllHits;

    [HideInInspector] public GameObject Target;
    [HideInInspector] public float AbilityCooldownTime = 7f;

    public delegate void FourthAbilityHandler(float value);
    public event FourthAbilityHandler FourthAbilityEvent;

    private float moveSpeed = 0.095f;
    private float acceleration = 0.095f;
    private float _distancePlayer;
    private SpriteRenderer childSpriteRenderer;
    private Vector2 end;
    private Vector2 _coursorPrefabPosition;
    private bool _canPull;
    public bool FixPrefab;
    private bool _isMoving;
    private bool _groundOrEnemy;
    private bool _cooldownIsActive;
    private bool _isDealDamage;
    private bool _canDrawDistancePrefab = true;
    private bool _canDoAbility;
    private GameObject _distancePrefab;
    private GameObject _newCoursorPrefab;

    private float healthOriginal;
    private float speedOriginal;

    protected override KeyCode ActivationKey => KeyCode.Alpha4;

    public bool CanPull
    {
        get { return _canPull; }
    }

    private void Start()
    {
        Distance = 3f * 1.94f + 1.94f * 0.5f; // дистанция щупалец
        _distancePlayer = 4f * 1.94f + 1.94f * 0.5f;//от кэрриган до щупалец
        AttackType = AttackType.OneAttack;
        AbilityType = AbilityType.DamageAbility;

        healthOriginal = transform.parent.GetComponent<HealthPlayer>().MaxHealth;
        speedOriginal = transform.parent.GetComponent<PlayerMove>().MoveSpeed;

    }

    private void Update()
    {
        HandleToggleAbility();
        Target = TargetParent;

        Vector2 checkPosition = transform.position;

        // Проверка наличия объекта с компонентом Slime в заданном радиусе
        bool isSlimePresent = CheckForSlime(checkPosition);

        if (isSlimePresent)
        {
            _player.GetComponent<HealthPlayer>().MaxHealth += healthOriginal * 0.05f;
            _player.GetComponent<PlayerMove>().MoveSpeed += speedOriginal * 0.1f;
        }
        else
        {
            _player.GetComponent<HealthPlayer>().MaxHealth = healthOriginal;
            _player.GetComponent<PlayerMove>().MoveSpeed = speedOriginal;

        }
    }


    bool CheckForSlime(Vector2 point)
    {
        Collider2D collider = Physics2D.OverlapCircle(point, 0.1f);
        if (collider != null)
        {
            slime slime = collider.gameObject.GetComponent<slime>();
            return slime != null;
        }

        return false;
    }

    protected override void HandleToggleAbility()
    {
        base.HandleToggleAbility();

        // Текущий код в методе Update
        _cooldownIsActive = CooldownButton.gameObject.activeSelf;

        if (Input.GetKeyDown(ActivationKey))
        {
            if (_cooldownIsActive)
            {
                CooldownButton.GetComponent<Button>().onClick.Invoke();
            }
        }

        if (Input.GetMouseButtonDown(0) && _player.GetComponent<PlayerMove>().IsSelect && Abilities.gameObject.activeSelf && ToggleAbility.enabled == true)
        {
            HandleLeftMouseButtonToggle();
        }

        if (Input.GetMouseButtonDown(1) && _player.GetComponent<PlayerMove>().IsSelect && Abilities.gameObject.activeSelf)
        {
            Vector2 cursorPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(cursorPosition, Vector2.zero);

            if (hit.collider != null && hit.collider.CompareTag("Enemies") && hit.collider.gameObject != gameObject)
            {
                if (ToggleAbility.enabled == true)
                {
                    HandleRightMouseButtonToggle();
                }
                else if (!ToggleAbility.enabled && _cooldownIsActive)
                {
                    CooldownButton.GetComponent<Button>().onClick.Invoke();
                }
            }

        }

        if (FixPrefab && NewAbilityPrefab != null)
        {
            RaycastHit2D hit = Physics2D.Raycast(_targetPosition, Vector2.zero);

            if (TargetParent == null)
            {
                if (childSpriteRenderer.color == new Color(childSpriteRenderer.color.r, childSpriteRenderer.color.g, childSpriteRenderer.color.b, 0.7f) && childSpriteRenderer != null && NewAbilityPrefab != null)
                {
                    childSpriteRenderer.color = new Color(childSpriteRenderer.color.r, childSpriteRenderer.color.g, childSpriteRenderer.color.b, 0.4f);

                }

                _targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

                if (TargetParent == null && hit.collider != null && hit.collider.CompareTag("Enemies") && hit.collider.gameObject != gameObject)
                {
                    
                        TargetParent = hit.collider.gameObject;
                        if (_canDrawDistancePrefab)
                        {
                            _distancePrefab = Instantiate(CircleDistancePrefab);
                            _distancePrefab.transform.SetParent(TargetParent.transform);
                            _distancePrefab.GetComponent<DrawCircle>().Draw(Distance - 1.9f / 4.5f);

                            _canDrawDistancePrefab = false;
                        }
                    
                }
            }

            if (hit.collider == null && !_groundOrEnemy)
            {
                NewAbilityPrefab.transform.position = _targetPosition;
                _groundOrEnemy = true;
            }
            else if (hit.collider != null && hit.collider.CompareTag("Enemies") && hit.collider.gameObject != gameObject && !_groundOrEnemy)
            {
                if ((NewAbilityPrefab.transform.position - hit.collider.transform.position).magnitude < Distance - 1.9f / 2f)
                {
                    TargetParent = hit.collider.gameObject;
                    NewAbilityPrefab.transform.position = TargetParent.transform.position;
                    NewAbilityPrefab.transform.SetParent(TargetParent.transform);
                    _groundOrEnemy = true;
                }
            }

            if (TargetParent != null && _canDoAbility)
            {
                float playerToTarget = (NewAbilityPrefab.transform.position - _player.transform.position).magnitude;

                if (_castCoroutine == null && playerToTarget < _distancePlayer - 1.9f / 2f && (NewAbilityPrefab.transform.position - TargetParent.transform.position).magnitude < Distance - 1.9f / 2f)
                {
                    _castCoroutine = StartCoroutine(CastTentacles());

                }

                if (CheckObstacleBetween(TargetParent.transform.position, NewAbilityPrefab.transform.position))
                {
                    ToggleAbility.isOn = false;
                    Destroy(NewAbilityPrefab);
                    return;
                }
            }
        }

        if (_canPull && NewAbilityPrefab != null && TargetParent != null)
        {
                TargetParent.GetComponent<PlayerMove>().CanMove = false;
            float activePsionica = _playerAbility.GetComponent<FiveConversion>().PsionicaActive;
            if (activePsionica > 0 && !_isDealDamage)
            {
                HandleActivePsionica(activePsionica);
                _isDealDamage = true;
            }
            if (!_isMoving)
            {
                end = NewAbilityPrefab.transform.position;
                StartCoroutine(MoveEnemy());
            }
        }

        if (TargetParent != null)
        {
            AddPsionicsForMoving();
        }
    }


    protected override void HandleToggleAbilityOn()
    {
        // Включенный ToggleAbility

        base.HandleToggleAbilityOn();

        if (FixPrefab == true && _cursorIsActive == false && _newCoursorPrefab == null)
        {
            Cursor.visible = true;
        }

        if (!FixPrefab)
        {
            HandlePrefabVisibility();
            if (NewAbilityPrefab != null)
            {
                childSpriteRenderer = FindChildSpriteRenderer(NewAbilityPrefab.transform);
                childSpriteRenderer.color = new Color(childSpriteRenderer.color.r, childSpriteRenderer.color.g, childSpriteRenderer.color.b, 0.7f);
                NewAbilityPrefab.transform.position = _targetPosition;
            }

            if (_player.GetComponent<PlayerMove>().IsSelect && Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                _targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                float playerToTarget = (_targetPosition - (Vector2)_player.transform.position).magnitude;


                if (TargetParent != null)
                {
                    float prefabToTarget = (TargetParent.transform.position - NewAbilityPrefab.transform.position).magnitude;

                    if (prefabToTarget < Distance - 1.9f / 2f)
                    {
                        FixPrefab = true;
                    }
                    else
                    {
                        Collider2D[] colliders = Physics2D.OverlapCircleAll(NewAbilityPrefab.transform.position, Distance);

                        if (colliders != null)
                        {
                            foreach (Collider2D collider in colliders)
                            {

                                if (collider.CompareTag("Enemies") && collider.gameObject != gameObject)
                                {
                                    FixPrefab = true;
                                    TargetParent = collider.gameObject;
                                    if (_distancePrefab != null)
                                    {
                                        Destroy(_distancePrefab);
                                        _canDrawDistancePrefab = true;
                                    }
                                    if (_canDrawDistancePrefab)
                                    {
                                        _distancePrefab = Instantiate(CircleDistancePrefab);
                                        _distancePrefab.transform.SetParent(TargetParent.transform);
                                        _distancePrefab.GetComponent<DrawCircle>().Draw(Distance - 1.9f / 4.5f);

                                        _canDrawDistancePrefab = false;
                                    }

                                    _targetCircle.SetActive(false);
                                    _targetCircle = TargetParent.GetComponent<PlayerMove>().CircleSelect;
                                    _targetCircle.SetActive(true);

                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (playerToTarget < _distancePlayer - 1.9f / 2f)
                    {
                        FixPrefab = true;
                    }
                    else
                    {
                        Collider2D[] colliders = Physics2D.OverlapCircleAll(NewAbilityPrefab.transform.position, _distancePlayer);

                        if (colliders != null)
                        {
                            foreach (Collider2D collider in colliders)
                            {

                                if (collider.CompareTag("Enemies") && collider.gameObject != gameObject)
                                {
                                    FixPrefab = true;
                                    TargetParent = collider.gameObject;
                                    if (_canDrawDistancePrefab)
                                    {
                                        _distancePrefab = Instantiate(CircleDistancePrefab);
                                        _distancePrefab.transform.SetParent(TargetParent.transform);
                                        _distancePrefab.GetComponent<DrawCircle>().Draw(Distance - 1.9f / 4.5f);

                                        _canDrawDistancePrefab = false;
                                    }

                                    break;
                                }
                            }
                        }
                    }
                }
            }
            _targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(_targetPosition, Vector2.zero);

            if (TargetParent == null && hit.collider != null && hit.collider.CompareTag("Enemies") && hit.collider.gameObject != gameObject)
            {
                if ((NewAbilityPrefab.transform.position - hit.collider.transform.position).magnitude < Distance - 1.9f / 2f)
                {
                    TargetParent = hit.collider.gameObject;
                    if (_canDrawDistancePrefab)
                    {
                        _distancePrefab = Instantiate(CircleDistancePrefab);
                        _distancePrefab.transform.SetParent(TargetParent.transform);
                        _distancePrefab.GetComponent<DrawCircle>().Draw(Distance - 1.9f / 4.5f);

                        _canDrawDistancePrefab = false;
                    }
                }
            }
        }

        if(_newCoursorPrefab != null)
        {
            Vector3 coursorPrefabPosition = new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, 0);
            _newCoursorPrefab.transform.position = coursorPrefabPosition;
            _newCoursorPrefab.GetComponentInChildren<DrawCircle>().Clear();
            Cursor.visible = false;
        }
        else if(_newCoursorPrefab == null && TargetParent == null)
        {
            Vector3 coursorPrefabPosition = new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, 0);
            _newCoursorPrefab = Instantiate(AbilityPrefab, coursorPrefabPosition, Quaternion.identity);   
        }

        if (TargetParent != null)
        {
            if(_newCoursorPrefab != null)
            {
                Destroy(_newCoursorPrefab.gameObject);
            }

            HandleDistanceToTarget();
        }
    }

    protected override void HandleToggleAbilityOff()
    {
        // Выключенный ToggleAbility
        base.HandleToggleAbilityOff();
        
        if (TargetParent == null && NewAbilityPrefab)
        {
            Destroy(NewAbilityPrefab);
            childSpriteRenderer = null;
        }

        if(_distancePrefab != null)
        {
            _distancePrefab.GetComponent<DrawCircle>().Clear();
            Destroy(_distancePrefab);
        }

        if(_newCoursorPrefab != null)
        {
            Destroy(_newCoursorPrefab);
        }
        _cursorIsActive = true;
        _canDrawDistancePrefab = true;
        _isDealDamage = false;
        _groundOrEnemy = false;
        FixPrefab = false;
        TargetParent = null;
        _canDoAbility = false;
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
                HandleAbilityType();
            }
        }
    }

    public override void ChangeBoolAndValues()
    {
        FixPrefab = true;
        if (NewAbilityPrefab == null)
        {
            NewAbilityPrefab = Instantiate(AbilityPrefab, TargetParent.transform.position, Quaternion.identity);
            childSpriteRenderer = FindChildSpriteRenderer(NewAbilityPrefab.transform);
        }
        if (NewAbilityPrefab != null)
        {
            NewAbilityPrefab.transform.position = TargetParent.transform.position;
            NewAbilityPrefab.transform.SetParent(TargetParent.transform);
        }
        if (_castCoroutine == null)
        {
            _castCoroutine = StartCoroutine(CastTentacles());
        }
        _groundOrEnemy = true;
    }

    public override void HandleDealDamageOrHeal()
    {
        _canDoAbility = true;
    }

    private void Reset()
    {
        if (NewAbilityPrefab)
        {
            Destroy(NewAbilityPrefab);
            childSpriteRenderer = null;

        }
        if (TargetParent != null && TargetParent.GetComponent<Rigidbody2D>())
        {
            TargetParent.GetComponent<PlayerMove>().CanMove = true;
            TargetParent.GetComponent<Rigidbody2D>().velocity = Vector3.zero;
        }
        Select.GetComponent<SelectObject>().CanSelect = true;
        _isDealDamage = false;
        _groundOrEnemy = false;
        FixPrefab = false;
        _canPull = false;
        _isMoving = false;
        NewAbilityPrefab = null;
        DrawCircle.Clear();
        CanDrawCircle = true;
        if (NewAbilityPrefab != null && TargetParent != null)
        {
            TargetParent.GetComponent<Rigidbody2D>().isKinematic = true;
            if (TargetParent.GetComponent<EnemyCollision>())
            {
                Destroy(TargetParent.GetComponent<EnemyCollision>());
            }
        }
        TargetParent = null;
        ToggleAbility.enabled = true;
        ToggleAbility.isOn = false;
    }

    private IEnumerator CastTentacles()
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
        CreateCastPrefab(1.2f);

        if (Abilities.GetComponent<GlobalCooldown>())
        {
            Abilities.GetComponent<GlobalCooldown>().StartGlobalCooldown();
        }

        yield return new WaitForSeconds(1.2f);

        if (!ToggleAbility.enabled)
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
        }
        ToggleAbility.enabled = false;

        _player.GetComponent<PlayerMove>().CanMove = true;
        if (NewAbilityPrefab != null)
        {
            childSpriteRenderer.color = new Color(childSpriteRenderer.color.r, childSpriteRenderer.color.g, childSpriteRenderer.color.b, 1f);
        }

        yield return new WaitForSeconds(0.3f);

        _canPull = true;
        StartCoroutine(DestroyPrefab());

    }

    private void HandleActivePsionica(float activePsionica)
    {
        StartCoroutine(DamageCooldown(activePsionica));
        GameObject _newDebaffPrefab;
        if (activePsionica >= 10 && activePsionica < 20)
        {
            List<BaseEffect> buffEffects = new List<BaseEffect>();
            Component[] allEffects = TargetParent.GetComponents<Component>();

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
        else if (activePsionica >= 30)
        {
            _newDebaffPrefab = Instantiate(HealthAbsorptionDebaff);
            _newDebaffPrefab.transform.SetParent(TargetParent.transform);
            _newDebaffPrefab.GetComponent<HealthAbsorption>().PercentageOfAbsorption = 0.4f;
            _newDebaffPrefab.GetComponentInChildren<BaffDebaffEffectPrefab>().StartCountdown(6);

        }
        else if (activePsionica >= 20 && activePsionica < 30)
        {
            _newDebaffPrefab = Instantiate(HealthAbsorptionDebaff);
            _newDebaffPrefab.transform.SetParent(TargetParent.transform);
            _newDebaffPrefab.GetComponent<HealthAbsorption>().PercentageOfAbsorption = 0.8f;
            _newDebaffPrefab.GetComponentInChildren<BaffDebaffEffectPrefab>().StartCountdown(6);


        }

        GetComponent<FiveConversion>().UseActivePsionica(activePsionica, Target);
    }

    private IEnumerator MoveEnemy()
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
        GameObject hitObject = null;
        _isMoving = true;
        float timer = 0f;
        moveSpeed = 0.095f;
        TargetParent.GetComponent<Rigidbody2D>().isKinematic = false;

        TargetParent.AddComponent<EnemyCollision>();

        if (TargetParent.GetComponent<EnemyCollision>() != null && TargetParent.GetComponent<EnemyCollision>().IsCollision)
        {
            moveSpeed /= 2f;
            acceleration /= 2f;
        }
        else
        {
            moveSpeed = 0.095f;
            acceleration = 0.095f;
        }

        while (NewAbilityPrefab != null && TargetParent != null && (Vector2)TargetParent.transform.position != end)
        {
            NewAbilityPrefab.transform.position = end;
            TargetParent.GetComponent<PlayerMove>().CanMove = false;

            timer += Time.deltaTime;
            if (hitObject != null)
            {
                acceleration = 0.095f / 2f;
            }
            if (timer >= 0.1f)
            {
                moveSpeed += acceleration;
                timer = 0f;
            }

            if (NewAbilityPrefab != null && TargetParent != null)
            {
                TargetParent.transform.position = Vector2.MoveTowards(TargetParent.transform.position, end, moveSpeed * Time.deltaTime * 10);


                AllHits = Physics2D.RaycastAll(TargetParent.transform.position, NewAbilityPrefab.transform.position, 1.25f);
                foreach (var hit in AllHits)
                {
                    if (hit.collider.gameObject != TargetParent && hit.collider.CompareTag("Enemies") || hit.collider.gameObject != TargetParent && hit.collider.CompareTag("Allies"))
                    {
                        hitObject = hit.collider.gameObject;
                        hitObject.GetComponent<PlayerMove>().CanMove = false;


                        hitObject.transform.position = Vector2.MoveTowards(hitObject.transform.position, end, moveSpeed * Time.deltaTime * 10);
                    }
                }
            }
            else
            {
                break;
            }

            yield return null;
        }


        _isMoving = false;
    }
    private IEnumerator DestroyPrefab()
    {
        yield return new WaitForSeconds(1.2f);

        Destroy(NewAbilityPrefab);
        Recharge();

        ToggleAbility.enabled = true;

    }
    private IEnumerator DamageCooldown(float activePsionica)
    {
        yield return new WaitForSeconds(0.1f);
        TargetParent.GetComponent<HealthPlayer>().TakeMagicDamage(activePsionica * 0.5f);
    }

    public void Recharge()
    {
        FourthAbilityEvent?.Invoke(0f);
        AbilityCooldownTime = 7;
        Reset();
        _castCoroutine = null;
        ToggleAbility.enabled = false;
        CooldownButton.gameObject.SetActive(true);

        while (AbilityCooldownTime > 0)
        {
            CooldownButton.GetComponentInChildren<TextMeshPro>().text = ((int)AbilityCooldownTime).ToString();
            AbilityCooldownTime -= Time.deltaTime;
        }

        CooldownButton.gameObject.SetActive(false);
        ToggleAbility.enabled = true;
        return;
    }

    SpriteRenderer FindChildSpriteRenderer(Transform parent)
    {

        foreach (Transform child in parent)
        {
            SpriteRenderer spriteRenderer = child.GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                return spriteRenderer;
            }
            else
            {
                spriteRenderer = FindChildSpriteRenderer(child);

                if (spriteRenderer != null)
                {
                    return spriteRenderer;
                }
            }
        }
        return null;
    }

    bool CheckObstacleBetween(Vector3 start, Vector3 end)
    {
        Vector2 direction = (end - start).normalized;
        float distance = Vector2.Distance(start, end);

        RaycastHit2D[] hits = Physics2D.BoxCastAll(start, new Vector2(1.9f, 1.9f), 0f, direction, distance, ObstacleLayerMask);

        foreach (RaycastHit2D hit in hits)
        {
            // Обработка столкновения
            Debug.Log("Collision with: " + hit.collider.name);
            return true;
        }

        return false;
    }
}
