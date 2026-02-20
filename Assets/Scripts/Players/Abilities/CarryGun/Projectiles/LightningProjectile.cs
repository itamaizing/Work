using Mirror;
using UnityEngine;
using System.Collections;

public class LightningProjectile : Projectiles
{
    [SerializeField] private float _speed = 20f;
    [SerializeField] private float _lifeTime = 5f;
    [SerializeField] private SphereCollider _collider;
    [SerializeField] private float _yOffset = 1.2f;

    private Character _target;
    private float _damage;
    private DamageType _damageType;

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

        StartCoroutine(FollowTarget());
        Destroy(gameObject, _lifeTime);
    }

    [Server]
    private IEnumerator FollowTarget()
    {
        while (_target != null)
        {
            Vector3 targetPos = _target.transform.position + Vector3.up * _yOffset;
            Vector3 dir = (targetPos - transform.position).normalized;

            transform.position += dir * _speed * Time.deltaTime;

            yield return null;
        }

        NetworkServer.Destroy(gameObject);
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
