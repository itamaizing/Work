using Mirror;
using System.Collections;
using UnityEngine;
using System;

public class ChainArrow : Projectiles
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float speed = 20f;
    [SerializeField] private float speedWithTarget = 4f;
    [SerializeField] private float stopDistance = 1.5f;
    [SerializeField] private LayerMask targetsLayer;
    [SerializeField] private Transform chainPoint;

    private Transform _playerTransform;
    private Vector3 _targetPoint;
    private float _maxDistance;
    private float _damage;

    private Coroutine _flyCoroutine;
    private Coroutine _returnCoroutine;
    private bool _isReturning = false;

    private Character _hookedTarget;
    private MoveComponent _hookedMove;

    private void OnDestroy()
    {
        MoveReset();
    }

    public void InitArrow(Vector3 targetPoint, Transform playerTransform, float maxDistance, float damage)
    {
        _targetPoint = targetPoint;
        _playerTransform = playerTransform;
        _maxDistance = maxDistance;
        _damage = damage;

        lineRenderer.positionCount = 2;
        _startPoint = transform.position;

        _flyCoroutine = StartCoroutine(FlyCoroutine());
    }

    private IEnumerator FlyCoroutine()
    {
        Vector3 direction = (_targetPoint - transform.position).normalized;
        _rb.velocity = Vector3.zero;
        _rb.AddForce(direction * speed, ForceMode.VelocityChange);
        float speedReturn = 0;

        while (!_isReturning)
        {
            UpdateLine();
            RotateArrow(direction);

            if (Vector3.Distance(_startPoint, transform.position) >= _maxDistance)
            {
                speedReturn = speed;
                break;
            }

            if (Physics.SphereCast(transform.position, 0.25f, _rb.velocity.normalized, out RaycastHit hit, 0.5f, targetsLayer))
            {
                if (hit.collider.TryGetComponent(out Character character))
                {
                    speedReturn = speedWithTarget;
                    AttachToTarget(character);
                    AddSkillCombo(character);
                    AddDisappointmentState(character);
                    ApplyDamage(_damage, DamageType.Physical, character.gameObject);
                }

                break;
            }

            yield return null;
        }

        StartReturn(speedReturn);
    }

    private void AttachToTarget(Character character)
    {
        _hookedTarget = character;
        _hookedMove = character.GetComponent<MoveComponent>();

        if (_hookedMove != null)
        {
            _hookedMove.CanMove = false;
        }

        transform.SetParent(_hookedTarget.transform);
        transform.localPosition = Vector3.zero;
        _rb.velocity = Vector3.zero;
        _rb.isKinematic = true;
    }

    private IEnumerator ReturnCoroutine(float speed)
    {
        _rb.velocity = Vector3.zero;

        if (_hookedTarget != null)
        {
            while (Vector3.Distance(_hookedTarget.transform.position, _playerTransform.position) > stopDistance)
            {
                if (_hookedMove != null)
                {
                    if (_hookedMove.connectionToClient != null)
                        _hookedMove.TargetRpcDoMove(_playerTransform.position, stopDistance);
                    else
                        _hookedMove.TestDoMove(_playerTransform.position, stopDistance); // Метод, для проверки на одном клиенте и Character, не синхронизированных по сети
                }

                UpdateLine();
                yield return null;
            }

            ReleaseTarget();
        }

        else
        {
            Vector3 dir = (_playerTransform.position - transform.position).normalized;
            _rb.isKinematic = false;
            _rb.AddForce(dir * speed, ForceMode.VelocityChange);

            while (Vector3.Distance(transform.position, _playerTransform.position) > stopDistance)
            {
                UpdateLine();
                yield return null;
            }
        }

        if (isServer)
            NetworkServer.Destroy(gameObject);
    }

    private void ReleaseTarget()
    {
        if (_hookedMove != null)
            _hookedMove.CanMove = true;

        _hookedTarget = null;
        _hookedMove = null;
        _isReturning = false;
    }

    private void StartReturn(float speed)
    {
        if (_isReturning) return;
        _isReturning = true;

        if (_flyCoroutine != null)
            StopCoroutine(_flyCoroutine);

        _returnCoroutine = StartCoroutine(ReturnCoroutine(speed));
    }

    private void UpdateLine()
    {
        if (_playerTransform == null || chainPoint == null || lineRenderer == null) return;

        lineRenderer.SetPosition(0, _playerTransform.position);
        lineRenderer.SetPosition(1, chainPoint.position);
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

        _skill.CmdApplyDamage(_damage, target);
    }

    private void MoveReset()
    {
        _dad.Move.StopLookAt();
        _dad.Move.CanMove = true;
    }

    private void AddSkillCombo(Character character)
    {
        if (character == null) return;
         
        if (_skill is ChainBlade skill) 
        {
            skill.ComboCounter.CmdAddSkill(character, skill);
            Debug.Log("[ChainBlade] Attack Passed");
        }
    }

    private void AddDisappointmentState(Character character)
    {
        if (isServer)
        {
            AddState(character);
        }
        else
        {
            CmdAddState(character);
        }
    }

    [Command]
    private void CmdAddState(Character character)
    {
        AddState(character);
    }

    [Command]
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
