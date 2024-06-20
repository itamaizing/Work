using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PoisonBallProjectile : MonoBehaviour
{
    //[SerializeField] PoisonBall poisonBall;

     public Character dad;
    [HideInInspector] public float energyDad;

    [SerializeField] private Rigidbody2D rigidbodyBall;
    [SerializeField] private GameObject _hitEffect;

    private Vector2 startPos;
    private Vector2 targetOrPointPosition;
    //private Vector2 _targetPos;
    private float fastMovementSpeed = 0.6f;  // Units per second
    private float slowMovementSpeed = 1.7f;  // Units per second
    private float durationStun = 1.2f;
    private float maxDistance = 6f;
    private float currentDamage = 35f;
    private bool isFast;
    // private Transform targetTransform;

    private void Start()
    {
        startPos = transform.position;
    }
    private void Update()
    {
        if (Vector2.Distance(transform.position, startPos) > maxDistance * GlobalVariable.cellSize)
        {
            Explode();
        }
    }
    public void MoveBall(Vector2 _targetOrPointPosition, bool _isFast)
    {
        targetOrPointPosition = Vector2.zero;
        targetOrPointPosition = _targetOrPointPosition;
        isFast = _isFast;
        float speed = isFast ? fastMovementSpeed : slowMovementSpeed;
        Debug.Log("Speed = " + speed);
        StartCoroutine(MoveCoroutine(targetOrPointPosition ,speed));
    }

    private IEnumerator MoveCoroutine(Vector2 target, float _speed)
    {
        rigidbodyBall.DOMove(target, _speed * maxDistance / GlobalVariable.cellSize).SetEase(Ease.Linear);
        yield return null;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == dad.gameObject || collision.CompareTag("Ability"))
        {
            return;
        }
        // damage
        if (collision.TryGetComponent<Character>(out var target))
        {
            // State duration 
            Energy energyLink = (Energy)dad.Stamina;

            energyLink.SumDamageMake(currentDamage);
            target.Health.TakeDamage(currentDamage, DamageType.Physical);
            target.CharacterState.AddState(new StunnedState(), durationStun, 0, States.Stun);

            GetComponent<Collider2D>().enabled = false;
            Debug.Log("Collider2D false");
        }
        //Explode();
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