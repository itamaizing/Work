using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpitPoisonProjectile : MonoBehaviour
{
    [HideInInspector] public Character dad;
    public float energyDad;

    [SerializeField] private Rigidbody2D _rb;
    [SerializeField] GameObject _hitEffect;
    [SerializeField] private float _force;
    [SerializeField] private float _distance = 5;
    private Vector2 startPos;

    private void Awake()
    {
        startPos = transform.position;
        _rb.AddForce(transform.up * _force, ForceMode2D.Impulse);
    }

    private void Update()
    {
        if (Vector2.Distance(transform.position, startPos) > _distance * GlobalVariable.cellSize)
        {
            Explode();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == dad.gameObject || collision.CompareTag("Ability"))
            return;
        //damage, blindness etc.
        if (collision.TryGetComponent<Character>(out var target))
        {
            // State duration 
            float duration = 1 + energyDad / 20;
            // Chance of blindness
            float chanceOfBlindness = 0.3f;
            // Random numbers for calculating the chance
            float randomNumber = Random.Range(0, 1.0f);

            target.Health.TakeDamage(4 + energyDad / 97, DamageType.Physical);
            
            if (randomNumber <= chanceOfBlindness)
            {
                target.CharacterState.AddState(new BlindnessState(), duration, 30, States.Blind);
            }
            GetComponent<Collider2D>().enabled = false;
        }
        Explode();
    }

    private void Explode()
    {
        if (_hitEffect != null)
        {
            GameObject hitEffect = Instantiate(_hitEffect, transform.position, Quaternion.identity);
            Destroy(hitEffect, 5f);
        }
        Destroy(gameObject);
    }
}
