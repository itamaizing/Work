using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class slime : MonoBehaviour
{
    [HideInInspector] public GameObject Uterus;

    private HealthPlayer _healthPlayer;
    private float _timer = 0f;
    private float _interval = 0.1f;

    void Start()
    {
        _healthPlayer = GetComponent<HealthPlayer>();
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
                _healthPlayer.TakePhisicDamage(2);
                _timer = 0f;
            }
        }
    }
}
