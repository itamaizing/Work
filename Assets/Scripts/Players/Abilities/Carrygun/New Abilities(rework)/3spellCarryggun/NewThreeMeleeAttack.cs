using Players.Abilities.Genjalf.Shield_Ability;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

public class NewThreeMeleeAttack : Ability
{
    [Header("Ability settings")]
    [SerializeField] private float _damageValue;
    [SerializeField] protected LayerMask ObstacleLayerMask;

    private Transform _target;
    private GameObject _player;
    private Coroutine _useJob;

    private Vector2 _playerPosition;
    private Vector2 _targetPosition;
    private Vector2 _enemyPosition;
    private Vector2 _initialPosition;
    private Collider2D[] _colliders;

    private bool _isInitialized = false;
    private float _amplitude = 1.5f;
    private float timePassed = 0f;


    private void Start() // временный костыль
    {
        _player = transform.parent.gameObject;
    }
    protected override void Cancel()
    {
        if (_useJob != null)
        {
            StopCoroutine(_useJob);
        }        
        ResetValue();
    }

    protected override void Cast()
    {
        _useJob = StartCoroutine(UseCoroutine());
    }
    private bool IsMouseInRadius()
    {
        float distance = Vector3.Distance(
            new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, transform.position.z),
            transform.position
            );

        return distance <= Radius;
    }
    private void ResetValue()
    {
        PlayerMove.CanMove = true;
        _target = null;
        _initialPosition = Vector2.zero;
        _targetPosition = Vector2.zero;
        _isInitialized = false;
        Debug.LogWarning("Reseted");
    }
    private bool CheckObstacleBetween(Vector3 start, Vector3 end)
    {
        //Проверка на наличие препятствия
        Vector2 direction = (end - start).normalized;
        float distance = Vector2.Distance(start, end);
        Debug.DrawLine(start, end, Color.white, 2.5f);
        RaycastHit2D[] hits =
            Physics2D.BoxCastAll(start, new Vector2(1f, 1f), 0f, direction, distance, ObstacleLayerMask);

        foreach (RaycastHit2D hit in hits)
        {
            return true;
        }

        return false;
    }
    private void Jump()
    {

        _player.GetComponent<MoveComponent>().CanMove = false;

        if (!_isInitialized)
        {
            _initialPosition = transform.position;

            Vector2 vectorToEnemy = _enemyPosition - _initialPosition;
            Vector2 Vector = vectorToEnemy.normalized * (vectorToEnemy.magnitude - 2f);
            _targetPosition = _initialPosition + Vector;

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

            _targetPosition = closestPoint;

            _isInitialized = true;
        }

        float targetToEnemy = (_enemyPosition - _targetPosition).magnitude;
        float initialPositionToEnemy = (_enemyPosition - _initialPosition).magnitude;

        if (initialPositionToEnemy > targetToEnemy)
        {
            timePassed += Time.deltaTime;
            float t = Mathf.Clamp01(timePassed / StreamingDuration); 

            float yOffset = _amplitude * Mathf.Sin(1 * Mathf.PI * t);
            Vector3 newPosition = Vector3.Lerp(_initialPosition, _targetPosition, t);
            newPosition.y += yOffset;


            _player.transform.position = newPosition;

            _playerPosition = (Vector2)_player.transform.position;

            if (_playerPosition == _targetPosition)
            {
                Debug.LogWarning("Test1");
                _target.GetComponent<HealthComponent>().TryTakeDamage(_damageValue, DamageType.Magical, AttackRangeType.MeleeAttack);
            }
        }
        else
        {
            Debug.LogWarning("Test2");
            _target.GetComponent<HealthComponent>().TryTakeDamage(_damageValue, DamageType.Magical, AttackRangeType.MeleeAttack);
        }
    }
    private IEnumerator UseCoroutine()
    {
        while (_target == null) //выбираем цель
        {
            if (Input.GetMouseButtonDown(0) && IsMouseInRadius())
            {
                RaycastHit2D[] rayHit = Physics2D.RaycastAll(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
                if (rayHit.Length > 0 && rayHit[0].transform.CompareTag("Enemies"))
                {
                    _target = rayHit[0].transform;
                }
            }
            yield return null;
        }
        if(!CheckObstacleBetween(_player.transform.position, _target.transform.position)) // если нет препятсвий прыгаем
        {
            timePassed = 0f;
            PlayerMove.CanMove = false;
            yield return GetCastDeleyCoroutine();

            _enemyPosition = _target.position;
            float time = 0;
            while (time < StreamingDuration)
            {
                time += Time.deltaTime;
                Jump();
                yield return null;
            }
        }

        ResetValue();
    }
}
