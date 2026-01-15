using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class LightSparkProjectile : Projectiles
{
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private float waveAmplitudeMin = 0.2f;
    [SerializeField] private float waveAmplitudeMax = 0.5f;
    [SerializeField] private float waveFrequencyMin = 1f;
    [SerializeField] private float waveFrequencyMax = 3f;
    [SerializeField] private float speed = 5f;

    [SerializeField] public ParticleSystem particleSystem;

    private float _waveAmplitude;
    private float _waveFrequency;
    private float _startTime;

    private GameObject _target;
    
    public event UnityAction<LightSparkProjectile, GameObject> EndPointReached;

    private void Awake()
    {
        if (_rb != null)
        {
            _rb.isKinematic = true;
        }
    }
    
    public void Init(GameObject target)
    {
        _waveAmplitude = Random.Range(waveAmplitudeMin, waveAmplitudeMax);
        _waveFrequency = Random.Range(waveFrequencyMin, waveFrequencyMax);

        _startTime = Time.time;

        _target = target;
    }

    public void StartFly()
    {
        if (particleSystem != null) particleSystem.Play();
        Destroy(gameObject, lifeTime);
    }

    private void FixedUpdate()
    {
        if (_rb == null || _target == null) return;

        Vector3 targetPosition = _target.transform.position + Vector3.up;
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;

        float elapsedTime = Time.time - _startTime;
        Vector3 forwardMovement = directionToTarget * (speed * Time.fixedDeltaTime);

        Vector3 rightLocal = Vector3.Cross(directionToTarget, Vector3.up).normalized;
        Vector3 upLocal = Vector3.Cross(rightLocal, directionToTarget).normalized;

        Vector3 waveOffset = upLocal * Mathf.Sin(elapsedTime * _waveFrequency) * _waveAmplitude;
        Vector3 sideOffset = rightLocal * Mathf.Sin(elapsedTime * _waveFrequency * 0.5f) * (_waveAmplitude * 0.5f);

        _rb.MovePosition(_rb.position + forwardMovement + waveOffset + sideOffset);

        if (particleSystem != null)
        {
            particleSystem.transform.position = transform.position;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent(out Character character))
        {
            EndPointReached?.Invoke(this, _target.gameObject);
            Destroy(gameObject, 0.1f);
        }
    }
}