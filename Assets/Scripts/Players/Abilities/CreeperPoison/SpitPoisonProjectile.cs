using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpitPoisonProjectile : MonoBehaviour
{
    SpitPoison spitPoison;
    //[HideInInspector] 
    public Character dad;
    [HideInInspector] public float energyDad;
    public Collider2D dadCollider;
    [SerializeField] private Collider2D projectileCollider;
    [SerializeField] private Rigidbody2D _rb;
    [SerializeField] GameObject _hitEffect;
    [SerializeField] private float _force;
    [SerializeField] private float _distance = 5;
    private Vector2 startPos;

    private void Awake()
    {
        startPos = transform.position;
        _rb.AddForce(transform.up * _force, ForceMode2D.Impulse);
        
        projectileCollider = GetComponent<Collider2D>();
        dadCollider = spitPoison._collider;

        Physics2D.IgnoreCollision(projectileCollider, dadCollider);
    }

    private void Update()
    {
        //if (Vector2.Distance(transform.position, startPos) > _distance * GlobalVariable.cellSize)
        //{
        //    Explode();
        //}
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == dad.gameObject || collision.CompareTag("Ability"))
        {
            Debug.Log("Ball is destroy");
            return;
        }
        // damage, blindness etc.
        if (collision.TryGetComponent<Character>(out var target))
        {
            // State duration 
            float duration = 1 + energyDad / 20;
            // Chance of blindness
            float chanceOfBlindness = 0.3f;
            float randomNumber = Random.Range(0, 1.0f);
            // damage dealing 
            float currentDamage = 4 + energyDad / 97;
            Energy energyLink = (Energy)dad.Stamina;

            energyLink.SumDamageMake(currentDamage);
            target.Health.TakeDamage(currentDamage, DamageType.Physical);
            if (randomNumber <= chanceOfBlindness)
            {
                target.CharacterState.AddState(new BlindnessState(), duration, 0, States.Blind);
                Debug.Log("State is true");
            }
            GetComponent<Collider2D>().enabled = false;
            Debug.Log("Collider2D false");
        }
        Explode();
    }

    private void Explode()
    {
        Debug.Log("Ball is destroy in Explode()");
        if (_hitEffect != null)
        {
            GameObject hitEffect = Instantiate(_hitEffect, transform.position, Quaternion.identity);
            Destroy(hitEffect, 5f);
        }
        Destroy(gameObject);
    }
}
