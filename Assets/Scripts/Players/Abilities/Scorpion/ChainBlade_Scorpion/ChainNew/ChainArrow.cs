using Mirror;
using System.Collections;
using UnityEngine;
using System;

public class ChainArrow : Projectiles
{
    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private float _speed = 20f;
    [SerializeField] private float _speedModifier = 1.2f;
    [SerializeField] private float _speedWithTarget = 4f;
    [SerializeField] private float _stopDistance = 1.5f;
    [SerializeField] private float _arrowYOffset = 1.5f;
    [SerializeField] private LayerMask _targetsLayer;
    [SerializeField] private Transform _chainPoint;

    private Transform _playerTransform;
    private Vector3 _targetPoint;
    private float _maxDistance;
    private float _damage;
    private float _flightTime = 0f;

    public Action<Character, float> OnHitTarget;
    private Coroutine _flyCoroutine;
    private Coroutine _returnCoroutine;
    private bool _isReturning = false;

    private Character _hookedTarget;
    private MoveComponent _hookedMove;
    private void OnDestroy()
    {
        if (_skill is ChainBlade chain)
        {
            chain.ChainBladeCastEnd(false);
            chain.Hero.Move.CanMove = true;
        }
    }

    public void Cleanup()
    {
        try
        {
            OnHitTarget = null;
            transform.SetParent(null);

            if (_hookedMove != null)
                _hookedMove.IsMoveBlocked = false;

            _hookedMove = null;
            _hookedTarget = null;

            _isReturning = false;

            if (_rb != null)
            {
                _rb.isKinematic = false;
                _rb.linearVelocity = Vector3.zero;
            }

            if (_lineRenderer != null)
                _lineRenderer.enabled = false;
        }
        catch { }
    }


    public void InitArrow(Vector3 targetPoint, Transform playerTransform, float maxDistance, float damage)
    {
        _targetPoint = targetPoint + Vector3.up * _arrowYOffset;
        _playerTransform = playerTransform;
        _maxDistance = maxDistance;
        _damage = damage;

        _lineRenderer.positionCount = 2;
        _startPoint = transform.position;

        _flyCoroutine = StartCoroutine(FlyCoroutine());
    }

    [Server]
    private void OnTriggerEnter(Collider other)
    {
        if (_isReturning) return;

        if (other.gameObject == _dad.gameObject) return;
        if (((1 << other.gameObject.layer) & _skill.TargetsLayers.value) == 0) return;

        if (other.TryGetComponent<Character>(out Character character))
        {
            AttachToTarget(character);
            AddSkillCombo(character);
            AddState(character);
            ApplyDamage(_damage, DamageType.Physical, character.gameObject);

            OnHitTarget?.Invoke(character, _flightTime);
        }
    }
    private IEnumerator FlyCoroutine()
    {
        Vector3 direction = (_targetPoint - transform.position).normalized;
        _rb.linearVelocity = Vector3.zero;
        _rb.AddForce(direction * _speed, ForceMode.VelocityChange);

        _flightTime = 0f;
        UpdateLine();

        while (!_isReturning)
        {
            _flightTime += Time.deltaTime;
            UpdateLine();
            RotateArrow(direction);

            if (Vector3.Distance(_startPoint, transform.position) >= _maxDistance)
            {
                break;
            }

            yield return null;
        }

        StartReturn();
    }

    private void AttachToTarget(Character character)
    {
        _hookedTarget = character;
        _hookedMove = character.GetComponent<MoveComponent>();

        if (_hookedMove != null) _hookedMove.IsMoveBlocked = true;
        if (_hookedTarget != null) _hookedTarget.Abilities.SetAbilitiesDisactive(true);
        transform.SetParent(character.transform);
        transform.localPosition = new Vector3(0f, 0.5f, 0f);
        _rb.linearVelocity = Vector3.zero;
        _rb.isKinematic = true;

        RpcAttachToTarget(character);
    }

    [ClientRpc]
    private void RpcAttachToTarget(Character character)
    {
        _hookedTarget = character;
        _hookedMove = character.GetComponent<MoveComponent>();

        if (_hookedMove != null)
            _hookedMove.IsMoveBlocked = true;

        transform.SetParent(character.transform);
        transform.localPosition = new Vector3(0f, 0.5f, 0f);
        _rb.linearVelocity = Vector3.zero;
        _rb.isKinematic = true;
    }

    private IEnumerator ReturnCoroutine(float speed)
    {
        _rb.linearVelocity = Vector3.zero;

        Vector3 dir = (_playerTransform.position - transform.position).normalized;
        _rb.isKinematic = false;
        _rb.AddForce(dir * speed, ForceMode.VelocityChange);

        while (Vector3.Distance(transform.position, _playerTransform.position + Vector3.up * _arrowYOffset) > _stopDistance)
        {
            UpdateLine();
            yield return null;
        }

        UpdateLine();

        if (isServer) NetworkServer.Destroy(gameObject);
    }

    private void StartReturn()
    {
        if (_isReturning) return;
        _isReturning = true;

        if (_flyCoroutine != null)
            StopCoroutine(_flyCoroutine);

        if (_skill is ChainBlade chain)
        {
            chain.Hero.Move.IsMoveBlocked = false;
            chain.Hero.Move.CanMove = false;
        }

        Debug.Log($"StartReturn Speed: {_speed}");
        _returnCoroutine = StartCoroutine(ReturnCoroutine(_speed));
    }

    private void UpdateLine()
    {
        if (_playerTransform == null || _chainPoint == null || _lineRenderer == null) return;

        _lineRenderer.SetPosition(0, _playerTransform.position + Vector3.up * _arrowYOffset);
        _lineRenderer.SetPosition(1, _chainPoint.position);
    }

    private void RotateArrow(Vector3 direction)
    {
        if (direction == Vector3.zero) return;
        transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(-90, 0, 0);
    }

    private void ApplyDamage(float damage, DamageType damageType, GameObject target)
    {
        Damage _damage = new Damage
        {
            Value = damage,
            Type = damageType
        };

        _skill.ApplyDamage(_damage, target);
    }

    private void AddSkillCombo(Character character)
    {
        if (character == null) return;

        if (_skill is ChainBlade skill)
        {
            skill.ComboCounter.AddSkill(character, skill);
        }
    }

    private void AddState(Character character)
    {
        if (character == null) return;

        float pullDistance = Vector3.Distance(_playerTransform.position, character.transform.position);

        if (pullDistance > 1f)
        {
            float duration = 1f;

            if (_skill is ChainBlade skill) if (skill.ComboCounter.IsFinalComboSkill(character, skill)) duration += 2f;

            int comboStacks = character.CharacterState.CheckStateStacks(States.ComboState);
            duration += comboStacks;

            character.CharacterState.AddState(States.DisappointmentState, duration, 0f, _dad.gameObject, _skill.name);
        }
    }

    private Vector3 _startPoint;
}