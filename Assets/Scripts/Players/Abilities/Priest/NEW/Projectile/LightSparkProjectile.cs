using Mirror;
using UnityEngine;

public class LightSparkProjectile : Projectiles
{
    [SerializeField] private float _lifeTime = 5f;

    private SparkOfLight _skillReference;
    private float _attackDelay;

    public void Init(HeroComponent dad, bool isLightMode, SparkOfLight skill, float distance, float attackDelay)
    {
        _dad = dad;
        _skillReference = skill;
        _distance = distance;
        _attackDelay = attackDelay;
    }

    public void StartFly(Vector3 direction)
    {
        float speed = _distance / _attackDelay;

        if (_rb != null) _rb.velocity = direction * speed;
        Destroy(gameObject, _lifeTime);
    }

    [Server]
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject != _dad.gameObject)
        {
            if (other.gameObject.TryGetComponent<Character>(out Character character))
            {
               _skillReference.HandleMode(character);
            }

            Destroy(gameObject, 0.1f);
        }
    }
}
