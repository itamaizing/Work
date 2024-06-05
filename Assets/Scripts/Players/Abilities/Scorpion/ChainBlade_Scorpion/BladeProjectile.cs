using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class BladeProjectile : MonoBehaviour
{
    private float throwForce = 10f;
    private Rigidbody2D _rb;
    private float _maxDistance;
    private Vector2 startPosition;

    public UnityEvent<GameObject> OnHit = new UnityEvent<GameObject>();


    public void Init(float maxDistance)
    {
        startPosition = transform.position;
        _maxDistance = maxDistance;
        StartCoroutine(DieOnDistance());
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.layer == 9 && collision.TryGetComponent<HealthComponent>(out HealthComponent enemyhealth))
        {
            enemyhealth.TryTakeDamage(10, DamageType.Physical, AttackRangeType.RangeAttack);
            SendMessage(collision.gameObject);
            HitPerfomed();
        }
    }
    

    private void SendMessage(GameObject target)
    {
        OnHit.Invoke(target);
    }
    public void ThrowBlade(Vector2 endPoint)
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.AddForce(endPoint.normalized * throwForce, ForceMode2D.Impulse);
    }

    private void HitPerfomed() 
    {
        //можно добавть визуал/партиклы при попадании
        Destroy(gameObject);
    }


    private void OnDestroy()
    {
        
    }

    private IEnumerator DieOnDistance()
    {
        float distance = Vector2.Distance(transform.position, startPosition);
        while(distance < _maxDistance)
        {
            distance = Vector2.Distance(transform.position, startPosition);
            yield return null;
        }

        OnHit.Invoke(null);
        Destroy(gameObject);
    }
}
