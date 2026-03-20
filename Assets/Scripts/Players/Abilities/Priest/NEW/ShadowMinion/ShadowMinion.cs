using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.AI;

public class ShadowMinion : NetworkBehaviour
{
    [SerializeField] private NavMeshAgent _agent;
    [SerializeField] private Animator _animator;
    [SerializeField] private float _lifetime = 6f;
    [SerializeField] private float _attackInterval = 1.2f;
    [SerializeField] private float _attackRange = 1.5f;
    [SerializeField] private float _destinationUpdateThreshold = 0.3f;

    private Character _target;
    private Skill _sourceSkill;
    private float _damagePercent = 0.02f;
    private const float SHACKLE_DURATION = 2f;
    private Vector3 _lastDestination;
    private Vector3 _previousPosition;
    private bool _initialized = false;
    private bool _reachedTarget = false;
    private bool _applyShackleOnExpire = false;

    private static readonly int _attackTrigger = Animator.StringToHash("Attack");
    private static readonly int _velocityX = Animator.StringToHash("X");
    private static readonly int _velocityZ = Animator.StringToHash("Y");

    public void InitOnClient(Character target, Skill sourceSkill, float speedMultiplier, bool applyShackleOnExpire)
    {
        _target              = target;
        _sourceSkill         = sourceSkill;
        _applyShackleOnExpire = applyShackleOnExpire;
        _initialized         = true;
        _previousPosition    = transform.position;

        if (_agent != null)
        {
            _agent.enabled               = true;
            _agent.updateRotation        = false;
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            _agent.avoidancePriority     = 0;
            
            if(applyShackleOnExpire)
                _agent.speed = sourceSkill.Hero.Move.CurrentSpeed * speedMultiplier;
            else
                _agent.speed = _target.Move.CurrentSpeed * speedMultiplier;
        }

        StartCoroutine(AttackJob());
        StartCoroutine(LifetimeJob());
    }
    private void Update()
    {
        if (!isOwned) return;
        if (!_initialized) return;
        if (_target == null || _target.IsDead) return;
        if (_agent == null || !_agent.isActiveAndEnabled) return;

        float distToTarget = Vector3.Distance(transform.position, _target.transform.position);

        if (distToTarget > _attackRange)
        {
            if (Vector3.Distance(_target.transform.position, _lastDestination) > _destinationUpdateThreshold)
            {
                _lastDestination = _target.transform.position;
                _agent.SetDestination(_lastDestination);
            }
        }
        else
        {
            if (_agent.hasPath)
                _agent.ResetPath();
        }

        UpdateLookAt();
        UpdateAnimationMovement();

        _previousPosition = transform.position;
    }

    private void UpdateLookAt()
    {
        Vector3 direction = _target.transform.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 360f * Time.deltaTime);
    }

    private void UpdateAnimationMovement()
    {
        if (_animator == null) return;

        Vector3 worldVelocity  = (transform.position - _previousPosition) / Time.deltaTime;
        Vector3 localVelocity  = transform.InverseTransformDirection(worldVelocity);

        float speedMultiplier  = _agent.speed > 0f ? 1f / _agent.speed : 1f;

        _animator.SetFloat(_velocityX, localVelocity.x * speedMultiplier);
        _animator.SetFloat(_velocityZ, localVelocity.z * speedMultiplier);
    }

    private IEnumerator AttackJob()
    {
        while (true)
        {
            if (_target == null || _target.IsDead) yield break;

            float distance = Vector3.Distance(transform.position, _target.transform.position);

            if (distance <= _attackRange)
                _animator.SetTrigger(_attackTrigger);

            yield return new WaitForSeconds(_attackInterval);
        }
    }

    public void OnAttackHit()
    {
        if (!_initialized) return;
        if (_target == null || _target.IsDead) return;

        float distance = Vector3.Distance(transform.position, _target.transform.position);
        if (distance > _attackRange) return;

        float damageValue;
        if (_applyShackleOnExpire)
        {
            damageValue = 8f;
        }
        else
        {
            damageValue = _target.Health.MaxValue * _damagePercent;
        }

        Damage damage = new Damage
        {
            Value = damageValue,
            Type  = DamageType.Magical,
        };

        _sourceSkill.CmdApplyDamage(damage, _target.gameObject);
    }

    [ClientRpc]
    private void RpcCheckAndApplyShackle()
    {
        if (!isOwned) return;
        if (!_applyShackleOnExpire) return;
        if (_target == null || _target.IsDead) return;

        float distance = Vector3.Distance(transform.position, _target.transform.position);
        if (distance > _attackRange * 1.5f) return;

        CmdApplyState(_target.gameObject);
    }
    
    [Command] 
    private void CmdApplyState(GameObject _target)
    {
        _target.GetComponent<Character>().CharacterState.AddState(States.ShackleState, SHACKLE_DURATION, 0, _target.gameObject, name);
        
        NetworkServer.Destroy(gameObject);
    }
    
    private IEnumerator LifetimeJob()
    {
        yield return new WaitForSeconds(_lifetime);
        OnLifeTimeEnd(_applyShackleOnExpire);
    }

    [Command]
    private void OnLifeTimeEnd(bool isShackleOnExpire)
    {
        RpcCheckAndApplyShackle();
        
        if(!isShackleOnExpire)
            NetworkServer.Destroy(gameObject);
    }
}
