using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] protected ParticleSystem _trailParticle;
    [SerializeField] protected ParticleSystem _destroyParticle;
    [SerializeField] protected float _speed = 10;
    [SerializeField] private bool _selfDestroyInEndPoint = true;
    [SerializeField] private float _lifeTime = 10;

    protected Transform _target;

    private void Start()
    {
        if(_trailParticle != null)
            _trailParticle = Instantiate(_trailParticle, transform.position, Quaternion.identity);
    }

    private void Update()
    {
        if (_trailParticle != null)
            _trailParticle.transform.position = transform.position;
    }

    protected virtual void OnDestroy()
    {
        if (_destroyParticle != null)
        {
            _destroyParticle = Instantiate(_destroyParticle, transform.position, Quaternion.identity);
            _destroyParticle.Play();
        }
        if (_trailParticle != null)
        {
            _trailParticle.Stop();
        }
    }

    public void StartFly(Vector3 position, bool directionMove = false)
    {
        var direction = (position - transform.position).normalized;

        Destroy(gameObject, _lifeTime);

        if (directionMove)
            StartCoroutine(InfiniteFlyCoroutine(direction));
        else
            StartCoroutine(FlyCoroutine(position));
    }

    public void StartFly(Transform transform)
    {
        _target = transform;

        Destroy(gameObject, _lifeTime);

        StartCoroutine(FlyCoroutine());
    }

    private void Rotate(Vector3 lookPoint)
    {
        transform.LookAt(lookPoint, Vector3.forward);
        transform.Rotate(new Vector3(90, 0, 0));
    }

    private IEnumerator FlyCoroutine(Vector3 position)
    {
        Rotate(position);

        while (transform.position != position)
        {
            transform.position = Vector2.MoveTowards(transform.position, position, _speed * Time.deltaTime);
            yield return null;
        }
        if (_selfDestroyInEndPoint)
            Destroy(gameObject);
    }

    private IEnumerator FlyCoroutine()
    {
        while (transform.position != _target.position)
        {
            Rotate(_target.position);

            transform.position = Vector2.MoveTowards(transform.position, _target.position, _speed * Time.deltaTime);
            yield return null;
        }
        if (_selfDestroyInEndPoint)
            Destroy(gameObject);
    }

    private IEnumerator InfiniteFlyCoroutine(Vector3 direction)
    {
        while (true)
        {
            transform.position += _speed * direction * Time.deltaTime;
            yield return null;
        }
    }
}
