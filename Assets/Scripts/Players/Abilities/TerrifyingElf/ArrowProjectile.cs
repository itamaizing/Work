using Mirror;
using UnityEngine;

public class ArrowProjectile : Projectiles
{
    [SerializeField] private float _speed = 10;
    [SerializeField] private float duration;
    [SerializeField] private float physicDamage;
    [SerializeField] private float minDamage;
    [SerializeField] private float maxDamage;
    [SerializeField] private bool _selfDestroyInEndPoint = true;
    [SerializeField] private bool _ArrowDark;
    [SerializeField] private float _lifeTime = 10;

    private Transform _target;
    private bool _superCharge = false;
    private bool _inTheRow = false;

    public Transform Target => _target;

    private void Start()
    {
        physicDamage = Random.Range(minDamage, maxDamage + 1);
    }

    public void StartFly(Vector3 targetPosition)
    { 
        Vector3 direction = (targetPosition - transform.position).normalized;

        if (_rb != null)
        {
            _rb.AddForce(direction * _speed, ForceMode.Impulse);
        }

        Destroy(gameObject, _lifeTime);
    }

    [Server]
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.TryGetComponent<Character>(out var target))
        {
            if (target.NetworkSettings.TeamIndex != _dad.NetworkSettings.TeamIndex)
            {
                ApplyDamage(physicDamage, DamageType.Physical, target);

                if (_ArrowDark)
                {
                    ApplyDamage(_skill.Damage, DamageType.Magical, target);
                    // target.CharacterState.AddState(States.InnerDarkness, duration, 0, _dad.gameObject, _skill.name);
                }

                Destroy(gameObject);
            }
        }

        if (collision.TryGetComponent<Object>(out var targetObject))
        {
            ApplyDamageObject(physicDamage, DamageType.Physical, targetObject);
            Destroy(gameObject);
        }
    }

    private void ApplyDamage(float damage, DamageType damageType, Character target)
    {
        Damage dmg = new Damage
        {
            Value = damage,
            Type = damageType,
        };
        target.Health.TryTakeDamage(ref dmg, _skill);
    }

    private void ApplyDamageObject(float damage, DamageType damageType, Object targetObject)
    {
        Damage dmg = new Damage
        {
            Value = damage,
            Type = damageType,
        };
        targetObject.ObjectHealth.TryTakeDamage(ref dmg, null);
    }
}
