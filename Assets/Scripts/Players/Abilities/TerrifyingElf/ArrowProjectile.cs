using Mirror;
using System.Collections;
using UnityEngine;

public class ArrowProjectile : Projectiles
{
    [SerializeField] private ParticleSystem _trailParticle;
    [SerializeField] private ParticleSystem _destroyParticle;
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
        if (_trailParticle != null)
            _trailParticle = Instantiate(_trailParticle, transform.position, Quaternion.identity);
        physicDamage = Random.Range(minDamage, maxDamage + 1);
    }

    private void Update()
    {
        if (_trailParticle != null)
            _trailParticle.transform.position = transform.position;
    }

    protected virtual void OnDestroy()
    {
        if (_destroyParticle != null)
        {
            _destroyParticle = Instantiate(_destroyParticle, transform.position, Quaternion.identity);
            _destroyParticle.Play();
        }
        if (_trailParticle != null)
        {
            _trailParticle.Stop();
        }
    }

    public void StartFly(Vector3 position, bool directionMove = false)
    {
        var direction = (position - transform.position).normalized;
        Destroy(gameObject, _lifeTime);
        StartCoroutine(directionMove ? InfiniteFlyCoroutine(direction) : FlyCoroutine(position));
    }

    private IEnumerator FlyCoroutine(Vector3 position)
    {
        while (transform.position != position)
        {
            transform.position = Vector2.MoveTowards(transform.position, position, _speed * Time.deltaTime);
            yield return null;
        }
        if (_selfDestroyInEndPoint) Destroy(gameObject);
    }

    private IEnumerator InfiniteFlyCoroutine(Vector3 direction)
    {
        while (true)
        {
            transform.position += _speed * direction * Time.deltaTime;
            yield return null;
        }
    }

    [Server]
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent<Character>(out var target))
        {
            if (target.NetworkSettings.TeamIndex != _dad.NetworkSettings.TeamIndex)
            {
                ApplyDamage(physicDamage, DamageType.Physical, target);

                if (_ArrowDark)
                {
                    ApplyDamage(_skill.Damage, DamageType.Magical, target);
                    //target.CharacterState.AddState(States.InnerDarkness, duration, 0, _dad.gameObject, _skill.name);
                }

                Destroy(gameObject);
            }
        }

        if (collision.TryGetComponent<Object>(out var targetObject))
        {
            if (targetObject.IndexTeam != _dad.NetworkSettings.TeamIndex)
            {
                Debug.Log($"Team Damage: {_dad.NetworkSettings.TeamIndex}");
                Debug.Log($"Team Tower: {targetObject.IndexTeam}");
                ApplyDamageObject(physicDamage, DamageType.Physical, targetObject);
                Destroy(gameObject);
            }

            else Destroy(gameObject);
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
