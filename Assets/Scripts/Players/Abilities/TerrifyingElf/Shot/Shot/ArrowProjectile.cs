using Mirror;
using UnityEngine;

public class ArrowProjectile : Projectiles
{
    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _lifeTime = 5f;
    [SerializeField] private bool _ArrowDark;
    [SerializeField] private float physicDamage;
    [SerializeField] private float minDamage;
    [SerializeField] private float maxDamage;
    [SerializeField] private float duration;

    private void Start()
    {
        physicDamage = Random.Range(minDamage, maxDamage + 1);
    }

    public void StartFly(Vector3 direction)
    {
        if (_rb != null)
        {
            _rb.velocity = direction * _speed;
        }

        Destroy(gameObject, _lifeTime);
    }

    [Server]
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IDamageable>(out IDamageable target))
        {
            if (other.TryGetComponent<UserNetworkSettings>(out UserNetworkSettings targetSettings))
            {
                if (targetSettings.TeamIndex != _dad.NetworkSettings.TeamIndex)
                {
                    ApplyDamage(physicDamage, DamageType.Physical, target);        
                }
            }

            else ApplyDamage(physicDamage, DamageType.Physical, target);

            Destroy(gameObject);
        }
    }

    private void ApplyDamage(float damage, DamageType damageType, IDamageable target)
    {
        Damage _damage = new Damage
        {
            Value = damage,
            Type = damageType,
        };
        target.TryTakeDamage(ref _damage, null);
    }

    //private void AddState(CharacterState targetState)
    //{
    //    targetState.AddState(States.InnerDarkness, duration, 0, _skill.Hero.gameObject, _skill.name);
    //}
}