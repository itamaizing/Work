using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class slime : MonoBehaviour
{
    [HideInInspector] public GameObject Uterus;

    private HealthComponent _healthComponent;
    private float _timer = 0f;
    private float _interval = 0.1f;

    void Start()
    {
        _healthComponent = GetComponent<HealthComponent>();
    }

    void Update()
    {
        if (Uterus == null)
        {
            ReduceHealth();
        }
    }

    private IEnumerator ReduceHealth()
    {
        yield return new WaitForSeconds(2);

        while (Uterus == null)
        {
            _timer += Time.deltaTime;

            if (_timer >= _interval)
            {
                _healthComponent.TryTakeDamage(2, DamageType.Physical, AttackRangeType.MeleeAttack);
                _timer = 0f;
            }
        }
    }
}
