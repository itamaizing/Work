using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sub_LavaPool_Scorpion : MonoBehaviour
{
    [SerializeField] private float _minDamagePerTick;
    [SerializeField] private float _maxDamagePerTick;
    [SerializeField] private DamageType _damageType;
    [SerializeField] private AttackRangeType _attackRangeType;

    private float _damageValue;
    private float _timeInterval = 1f;
    private float _lifeTime = 3f;

    private List<HealthComponent> _enemies = new List<HealthComponent>();

    public void Init()
    {
        _damageType = DamageType.Magical;
        _attackRangeType = AttackRangeType.Inner;
        StartCoroutine(DealDamageOvertime());
        StartCoroutine(LifeTimeTimer());
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.TryGetComponent<HealthComponent>(out  HealthComponent enemy) && collision.gameObject.CompareTag("Enemies")) 
        {
            _enemies.Add(enemy);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if(collision.TryGetComponent<HealthComponent>(out HealthComponent enemy) && collision.gameObject.CompareTag("Enemies"))
        {
            _enemies.Remove(enemy);
        }
    }

    private IEnumerator DealDamageOvertime()
    {
        yield return new WaitForSeconds(0.1f);

        while (true)
        {
            Debug.LogWarning("damageTickPool");
            _damageValue = Random.Range(_minDamagePerTick, _maxDamagePerTick);
            foreach (var item in _enemies)
            {
                item.TryTakeDamage(_damageValue, _damageType, _attackRangeType);
            }

            yield return new WaitForSeconds(_timeInterval);

        }
    }

    private IEnumerator LifeTimeTimer()
    {
        yield return new WaitForSeconds(_lifeTime);
        Destroy(gameObject);
    }

}
