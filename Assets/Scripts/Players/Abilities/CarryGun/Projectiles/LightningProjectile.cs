using Mirror;
using System.Collections;
using UnityEngine;

public class LightningProjectile : Projectiles
{
    [SerializeField] private float _speed = 15f;
    [SerializeField] private float _yOffset = 1.2f;
    [SerializeField] private float _lifeTime = 5f;
    [SerializeField] private SphereCollider _collider;

    private Character _target;
    private float _damage;
    private DamageType _damageType;
    private Transform _followTarget;

    private Vector3 _startPosition;
    private bool _isFollowingTarget = false;

    public void Init(
        Character dad,
        float energy,
        bool lastHit,
        Skill skill,
        Character target,
        float damage,
        DamageType damageType)
    {
        base.Init(dad, energy, lastHit, skill);

        if (!isServer) return;

        _target = target;
        _damage = damage;
        _damageType = damageType;
        _initialized = true;

        _collider.enabled = true;
        Destroy(gameObject, _lifeTime);
    }

    public void StartFly(Transform target)
    {
        Debug.Log("1");
        _startPosition = transform.position;
        _followTarget = target;
        _isFollowingTarget = true;
        StartCoroutine(FollowTargetCoroutine());
    }

    private IEnumerator FollowTargetCoroutine()
    {
        while (_isFollowingTarget && _followTarget != null)
        {
            Vector3 targetPos = _followTarget.position;
            Vector3 dir = (targetPos - transform.position).normalized;
            if (_rb != null) _rb.linearVelocity = dir * _speed;

            yield return null;
        }
    }

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (!_initialized) return;
        if (_target == null) return;

        if (!other.TryGetComponent(out Character character)) return;
        if (character != _target) return;

        ApplyDamage(character.gameObject);

        NetworkServer.Destroy(gameObject);
    }

    [Server]
    private void ApplyDamage(GameObject target)
    {
        if (_skill == null) return;

        Damage damage = new Damage
        {
            Value = _damage,
            Type = _damageType,
            School = _skill.School
        };

        _skill.ApplyDamage(damage, target);
    }
}
