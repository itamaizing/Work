using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Uterus : MonoBehaviour
{
    [SerializeField] private GameObject slimePrefab;

    private HealthComponent _healthComponent;
    private float _timerReduce = 0f;
    private float _intervalReduce = 0.1f;

    [HideInInspector] public List<GameObject> Slimes = new List<GameObject>();

    private GameObject _newSlimePrefab;
    private float _timerPrefab = 0f;
    private float _intervalPrefab = 1f;
    private float _radius = 1.5f * 1.94f;

    private bool _canTakeDamage = true;
    private Coroutine _AddHealthCoroutine;

    void Start()
    {
        _healthComponent = GetComponent<HealthComponent>();

    }

    void Update()
    {
        if (_canTakeDamage)
        {
            ReduceHealth();
            CreateSlime();
        }

        if (gameObject.GetComponentInChildren<EmbryonDebuff>())
        {
            _AddHealthCoroutine = StartCoroutine(AddHealth());
            _canTakeDamage = false;
        }
        else
        {
            _AddHealthCoroutine = null;
            _canTakeDamage = true;
        }
    }

    private void ReduceHealth()
    {
        _timerReduce += Time.deltaTime;

        if (_timerReduce >= _intervalReduce)
        {
            _healthComponent.TryTakeDamage(2, DamageType.Physical, AttackRangeType.MeleeAttack);
            _timerReduce = 0f;
        }
    }

    private void CreateSlime()
    {
        _timerPrefab += Time.deltaTime;

        if( _timerPrefab >= _intervalPrefab)
        {
            for (int i = 0; i < Slimes.Count; i++)
            {
                //Slimes[i].GetComponent<HealthComponent>()._currentHealth = 10f;
            }

            Vector2 randomPoint = GetRandomPointInRadius(transform.position, _radius);
            _newSlimePrefab = Instantiate(slimePrefab, randomPoint, Quaternion.identity);
            _newSlimePrefab.GetComponent<slime>().Uterus = gameObject;
            Slimes.Add(_newSlimePrefab);

            _timerPrefab = 0;
        }
    }

    Vector2 GetRandomPointInRadius(Vector2 center, float radius)
    {
        float angle = Random.Range(0f, 2f * Mathf.PI);
        float distance = Mathf.Sqrt(Random.Range(0f, 1f)) * radius;

        float x = center.x + distance * Mathf.Cos(angle);
        float y = center.y + distance * Mathf.Sin(angle);

        return new Vector2(x, y);
    }

    private IEnumerator AddHealth()
    {
        yield return new WaitForSeconds(3);
    }
}
