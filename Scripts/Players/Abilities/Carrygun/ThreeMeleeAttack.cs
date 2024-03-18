using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ThreeMeleeAttack : AbilityBase
{
    [Header("Ability properties")] [SerializeField]
    private Renderer[] _renderers;

    [HideInInspector] public GameObject Target;

    public delegate void ThirdAbilityHandler(float value);

    public event ThirdAbilityHandler ThirdAbilityEvent;

    private bool _isInitialized = false;
    private bool _canJump = false;
    private bool _damageDealt = false;
    private bool _castPrefab;
    private Vector2 _playerPosition;
    private Vector2 _enemyPosition;
    private Vector2 _initialPosition;
    private Vector2 _target;
    private float _distanceToEnemy;
    private float _startTime;
    private float _durationJump = 0.4f;
    private float _amplitude = 1.5f;
    private Collider2D[] _colliders;

    protected override KeyCode ActivationKey => KeyCode.Alpha3;

    private void Start()
    {
        Distance = 5f * 1.9f;
        AttackType = AttackType.OneAttack;
        AbilityType = AbilityType.DamageAbility;
        AttackRangeType = AttackRangeType.MeleeAttack;
    }

    private void Update()
    {
        HandleToggleAbility();
        Target = TargetParent;
    }

    protected override void HandleToggleAbility()
    {
        base.HandleToggleAbility();

        // Текущий код в методе Update

        if (Input.GetMouseButtonDown(0) && ToggleAbility.gameObject.activeSelf && ToggleAbility.enabled &&
            _player.GetComponent<PlayerMove>().IsSelect)
        {
            HandleLeftMouseButtonToggle();
        }

        if (_canJump)
        {
            foreach (var item in _renderers)
            {
                item.sortingLayerID = SortingLayer.NameToID("Jump");
            }
        }
    }

    protected override void HandleToggleAbilityOn()
    {
        // Включенный ToggleAbility
        base.HandleToggleAbilityOn();

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

        _castPrefab = false;
        _castCoroutine = null;
        TargetParent = null;
        _isInitialized = false;
        _canJump = false;
        _damageDealt = false;
    }

    public override void OnLeftDoubleClick()
    {
        if (ShouldUseToggleTarget() || _isInputDoubleClick)
        {
            StartCoroutine(ToggleDoubleClick());
        }

        else if (AbilityTypeManager.ActiveAbilityType == 1 && _player.GetComponent<PlayerMove>().IsSelect &&
                 Abilities.gameObject.activeSelf)
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

    public override void OnRightDoubleClick()
    {
    }

    public override void ChangeBoolAndValues()
    {
        _isInitialized = false;
        _damageDealt = false;
        _initialPosition = Vector2.zero;
        _target = Vector2.zero;

        if (NewAbilityPrefab != null)
        {
            Destroy(NewAbilityPrefab);
        }
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
        HandleCastJump();
        Destroy(NewAbilityPrefab);
    }

    private void HandleCastJump()
    {
        // Проверка дистанции и каст
        _playerPosition = _player.transform.position;
        _enemyPosition = TargetParent.transform.position;
        _distanceToEnemy = (_enemyPosition - _playerPosition).magnitude;

        if (_distanceToEnemy <= Distance)
        {
            if (!CheckObstacleBetween(_player.transform.position, TargetParent.transform.position))
            {
                DrawCircle.Clear();

                _castCoroutine = StartCoroutine(CastJump());
                if (_castPrefab == false)
                {
                    CreateCastPrefab(0.3f);
                    _castPrefab = true;
                }
            }
        }
    }

    private void Jump()
    {
        if (_canJump && ToggleAbility.isOn == true)
        {
            ToggleAbility.enabled = false;
            _player.GetComponent<PlayerMove>().CanMove = false;

            if (!_isInitialized)
            {
                _initialPosition = transform.position;
                _startTime = Time.time;

                Vector2 vectorToEnemy = _enemyPosition - _initialPosition;
                Vector2 Vector = vectorToEnemy.normalized * (vectorToEnemy.magnitude - 2f);
                _target = _initialPosition + Vector;

                Vector2 closestPoint = Vector2.zero;
                float closestDistance = float.MaxValue;

                for (float angle = 0f; angle < 360f; angle += 1f)
                {
                    float x = _enemyPosition.x + 2f * Mathf.Cos(angle * Mathf.Deg2Rad);
                    float y = _enemyPosition.y + 2f * Mathf.Sin(angle * Mathf.Deg2Rad);
                    Vector2 testPoint = new Vector2(x, y);

                    bool isObstacleNearby = false;

                    _colliders = Physics2D.OverlapCircleAll(testPoint, 1f);

                    foreach (Collider2D collider in _colliders)
                    {
                        if (collider.CompareTag("Obstacle"))
                        {
                            break;
                        }
                    }

                    if (!isObstacleNearby)
                    {
                        float distanceToInitial = Vector2.Distance(testPoint, _initialPosition);
                        if (distanceToInitial < closestDistance)
                        {
                            closestPoint = testPoint;
                            closestDistance = distanceToInitial;
                        }
                    }
                }

                _target = closestPoint;

                _isInitialized = true;
            }

            float targetToEnemy = (_enemyPosition - _target).magnitude;
            float initialPositionToEnemy = (_enemyPosition - _initialPosition).magnitude;

            if (initialPositionToEnemy > targetToEnemy)
            {
                float timePassed = Time.time - _startTime;
                float t = Mathf.Clamp01(timePassed / _durationJump);

                float yOffset = _amplitude * Mathf.Sin(1 * Mathf.PI * t);
                Vector3 newPosition = Vector3.Lerp(_initialPosition, _target, t);
                newPosition.y += yOffset;

                _player.transform.position = newPosition;

                if (_playerPosition == _target)
                {
                    MakeDamageWithJump();
                }
            }
            else
            {
                MakeDamageWithoutJump();
            }
        }
    }

    private void MakeDamageWithJump()
    {
        if (!_damageDealt && _distanceToEnemy < 2.1f)
        {
            float damage = UnityEngine.Random.Range(11, 14);

            float maxDistance = Distance - 2f;
            float distanceToTarget = (_target - _initialPosition).magnitude;


            if (distanceToTarget >= maxDistance && TargetParent != null)
            {
                TargetParent.GetComponent<HealthPlayer>().TakePhisicDamage(damage + (damage * 0.2f));
                _player.GetComponent<PsionicaMelee>().MakePsionica(damage + (damage * 0.2f));
                _damageDealt = true;
                ThirdAbilityEvent?.Invoke(damage + (damage * 0.2f));
            }
            else if (distanceToTarget < maxDistance && TargetParent != null)
            {
                float numberOfBody = distanceToTarget / 1.9f;
                TargetParent.GetComponent<HealthPlayer>()
                    .TakePhisicDamage(damage + (damage * 0.005f * (numberOfBody / 0.1f)));
                _player.GetComponent<PsionicaMelee>().MakePsionica(damage + (damage * 0.005f * (numberOfBody / 0.1f)));
                _damageDealt = true;
                ThirdAbilityEvent?.Invoke(damage + (damage * 0.005f * (numberOfBody / 0.1f)));
            }
        }
        else
        {
            StartCoroutine(Stop());
        }
    }

    private void MakeDamageWithoutJump()
    {
        if (!_damageDealt && TargetParent != null && _distanceToEnemy < 2.1f)
        {
            float damage = UnityEngine.Random.Range(11, 13);
            TargetParent.GetComponent<HealthPlayer>().TakePhisicDamage(damage + (damage * 0.1f));
            _player.GetComponent<PsionicaMelee>().MakePsionica(damage + (damage * 0.1f));
            _damageDealt = true;
            ThirdAbilityEvent?.Invoke(damage + (damage * 0.1f));
        }
        else
        {
            StartCoroutine(Stop());
        }
    }

    private IEnumerator CastJump()
    {
        _player.GetComponent<PlayerMove>().CanMove = false;

        if (Abilities.GetComponent<GlobalCooldown>())
        {
            Abilities.GetComponent<GlobalCooldown>().StartGlobalCooldown();
        }

        yield return new WaitForSeconds(0.3f);

        _player.GetComponent<PlayerMove>().CanMove = true;
        _canJump = true;

        Jump();
    }

    private bool CheckObstacleBetween(Vector3 start, Vector3 end)
    {
        //Проверка на наличие препятствия
        Vector2 direction = (end - start).normalized;
        float distance = Vector2.Distance(start, end);

        RaycastHit2D[] hits =
            Physics2D.BoxCastAll(start, new Vector2(1f, 1f), 0f, direction, distance, ObstacleLayerMask);

        foreach (RaycastHit2D hit in hits)
        {
            return true;
        }

        return false;
    }

    private IEnumerator Stop()
    {
        yield return new WaitForSeconds(0.3f);

        _player.GetComponent<PlayerMove>().CanMove = true;

        yield return new WaitForSeconds(0.4f);

        foreach (var item in _renderers)
        {
            item.sortingLayerID = SortingLayer.NameToID("Default");
        }

        ToggleAbility.isOn = false;
        ToggleAbility.enabled = true;
        _canJump = false;

        yield break;
    }
}