using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sub_LavaPool_Scorpion : MonoBehaviour
{
    [SerializeField] private float _minDamagePerTick;
    [SerializeField] private float _maxDamagePerTick;
    [SerializeField] private DamageType _damageType;
    [SerializeField] private AttackRangeType _attackRangeType;
    [SerializeField] private LayerMask _layerMask;

    private float _damageValue;
    private float _timeInterval = 1f;
    private float _lifeTime = 3f;
    private Material _material;

    private List<HealthComponent> _enemies = new List<HealthComponent>();

    //private void Start()
    //{
    //    _material = GetComponent<SpriteRenderer>().material;        
    //}
    private void Start()
    {
        _material = GetComponent<SpriteRenderer>().material;
    }
    private void Update()
    {
        
        _material.mainTextureOffset = new Vector2(_material.mainTextureOffset.x + 0.01f * Time.deltaTime, _material.mainTextureOffset.y + 0.01f * Time.deltaTime);
    }
    public void Init()
    {
        _damageType = DamageType.Magical;
        _attackRangeType = AttackRangeType.MeleeAttack;
        StartCoroutine(DealDamageOvertime());
        Destroy(gameObject, _lifeTime);
        _material = GetComponent<SpriteRenderer>().material;
        //StartCoroutine(LifeTimeTimer());
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.TryGetComponent<HealthComponent>(out HealthComponent enemy) /*&& collision.gameObject.layer == _layerMask*/) 
        {
            _enemies.Add(enemy);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if(collision.TryGetComponent<HealthComponent>(out HealthComponent enemy) && collision.gameObject.layer == 9)
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
        float x =0, y = 0;
        while (true)
        {
            _material.mainTextureOffset = new Vector2(x, y);
            x++; y++;
            yield return null;
        }
    }

}
