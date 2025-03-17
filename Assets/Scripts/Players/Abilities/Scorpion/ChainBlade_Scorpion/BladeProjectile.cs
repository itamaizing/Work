using Mirror;
using UnityEngine;

public class BladeProjectile : Projectiles
{
    [Header("Blade Projectile Settings")]
    [SerializeField] private float _damageMin = 3f;
    [SerializeField] private float _damageMax = 5f;

    [SerializeField] private float _lifeTime = 3f;

    [Header("Chain Settings")]
    [SerializeField] private Transform chainLinkPoint;

    private float _currentLifeTime;
    private bool isChain = false;

    public Transform ChainLinkPoint => chainLinkPoint != null ? chainLinkPoint : transform;

    public void StartFly(Vector3 direction)
    {
        if (_rb != null) _rb.velocity = direction * _force;

        Destroy(gameObject, _lifeTime);
    }

    [Server]
    private void OnTriggerEnter(Collider other)
    {
        if (!_initialized) return;

        if (other.TryGetComponent(out Character target))
        {
            if (target == _dad) return;

            ApplyDamage(target);

            Destroy(gameObject);
        }
    }

    private void ApplyDamage(Character target)
    {
        Damage damage = new Damage
        {
            Value = Random.Range(_damageMin, _damageMax),
            Type = DamageType.Physical
        };

        _skill.ApplyDamage(damage, target.gameObject);
    }

    public override void Init(HeroComponent dad, float energy, bool lastHit, Skill skill)
    {
        base.Init(dad, energy, lastHit, skill);

        _rb.velocity = transform.forward * _force;
        isChain = lastHit;
    }
}
