using Mirror;
using UnityEngine;

public class ArrowProjectile : Projectiles
{
    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _lifeTime = 5f;
    [SerializeField] private bool _arrowDark;
    [SerializeField] private float physicDamage;
    [SerializeField] private float magDamage;
    [SerializeField] private float minDamage;
    [SerializeField] private float maxDamage;
    [SerializeField] private float duration;
    [SerializeField] private DamageType damageTypePhysics;

    public bool ArrowDark { get => _arrowDark; set => _arrowDark = value; }

    private void Start()
    {
        physicDamage = Random.Range(minDamage, maxDamage + 1);
    }

    public void StartFly(Vector3 direction)
    {
        if (_rb != null) _rb.velocity = direction * _speed;

        Destroy(gameObject, _lifeTime);
    }

    [Server]
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject != _dad.gameObject)
        {
            if (((1 << other.gameObject.layer) & _skill.TargetsLayers.value) != 0)
            {
                if (other.gameObject.TryGetComponent<ObjectHealth>(out ObjectHealth objectHealth)) if (objectHealth.ResistMagicDamage >= 100 && _arrowDark) return;

                ApplyEnemy(other);
            }

            Destroy(gameObject);
        }
    }

    //private void TargetApply(Collider other)
    //{
    //    //if (other.TryGetComponent<IDamageable>(out IDamageable target))
    //    //{
    //    //    //if (other.TryGetComponent<UserNetworkSettings>(out UserNetworkSettings userNetworkSettings))
    //    //    //{
    //    //    //    if (userNetworkSettings.TeamIndex != _dad.NetworkSettings.TeamIndex)
    //    //    //    {
    //    //    //        ApplyEnemy(other, target);
    //    //    //    }
    //    //    //}

    //    //    if (other.gameObject != _dad.gameObject && ((1 << other.gameObject.layer) & _skill.TargetsLayers.value) != 0) ApplyEnemy(other);
    //    //}
    //}

    #region ApplyEnemy
    private void ApplyEnemy(Collider collider)
    {
        if (_arrowDark)
        {
            _skill.Damage = physicDamage;

            ApplyDamage(physicDamage, damageTypePhysics, collider.gameObject);

            bool physicalDamageApplied = TryApplyDamage(damageTypePhysics, _skill.AttackRangeType, collider.gameObject);
            if (physicalDamageApplied) return;

            _skill.Damage = magDamage;
            ApplyDamage(magDamage, _skill.DamageType, collider.gameObject);

            if (collider.TryGetComponent<Character>(out Character character)) character.CharacterState.AddState(States.InnerDarkness, duration, 0, _skill.Hero.gameObject, _skill.name);
        }

        else
        {
            _skill.Damage = physicDamage;
            ApplyDamage(physicDamage, damageTypePhysics, collider.gameObject);
        }
    }
    #endregion

    private void ApplyDamage(float damage, DamageType damageType, GameObject target)
    {
        Damage _damage = new Damage
        {
            Value = damage,
            Type = damageType
        };

        _skill.ApplyDamage(_damage, target);
    }

    private bool TryApplyDamage(DamageType damageType, AttackRangeType attackRangeType, GameObject target)
    {
        if (target.TryGetComponent<Health>(out Health health)) return health.TryEvade(damageType, attackRangeType);

        return false;
    }
}
