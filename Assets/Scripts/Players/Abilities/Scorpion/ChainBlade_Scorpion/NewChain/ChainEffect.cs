using UnityEngine;

public class ChainEffect : MonoBehaviour
{
    [Header("Chain Points")]
    public GameObject point1; // Игрок
    public GameObject point2; // BladeProjectile

    [Header("Particle Settings")]
    [SerializeField] private float particleSpeed = 20f;
    [SerializeField] private int particleCount = 50;
    [SerializeField] private float linkSpacing = 0.5f;
    [SerializeField] private float waveFrequency = 2f;
    [SerializeField] private float waveAmplitude = 0.0f;

    [Header("Offset Settings")]
    [SerializeField] private float heightOffset = 0.5f;

    private ParticleSystem chainParticleSystem;
    private ParticleSystem.Particle[] particles;

    private Camera mainCamera;

    public void Initialize(GameObject startPoint, GameObject endPoint)
    {
        point1 = startPoint;
        point2 = endPoint;

        Activate();
    }

    public void SetTarget(GameObject endPoint)
    {
        point2 = endPoint;
    }

    public void Activate()
    {
        if (chainParticleSystem != null)
        {
            chainParticleSystem.Play();
        }
    }

    public void Deactivate()
    {
        if (chainParticleSystem != null)
        {
            chainParticleSystem.Stop();
        }
    }

    private void Start()
    {
        chainParticleSystem = GetComponent<ParticleSystem>();
        mainCamera = Camera.main;

        var mainModule = chainParticleSystem.main;
        mainModule.maxParticles = particleCount;

        var emissionModule = chainParticleSystem.emission;
        emissionModule.rateOverTime = 0;
        emissionModule.rateOverDistance = 0;

        particles = new ParticleSystem.Particle[particleCount];
    }

    private void LateUpdate()
    {
        if (point1 == null || point2 == null)
            return;

        UpdateChain();
    }

    private void UpdateChain()
    {
        Vector3 startPosition = point1.transform.position + Vector3.up * heightOffset;
        Vector3 endPosition = point2.transform.position + Vector3.up * heightOffset;

        Vector3 direction = (endPosition - startPosition).normalized;
        float distance = Vector3.Distance(startPosition, endPosition);

        int linksCount = Mathf.CeilToInt(distance / linkSpacing);
        linksCount = Mathf.Clamp(linksCount, 2, particleCount);

        int activeParticles = linksCount;

        for (int i = 0; i < activeParticles; i++)
        {
            float t = (float)i / (activeParticles - 1);
            Vector3 basePosition = Vector3.Lerp(startPosition, endPosition, t);

            float waveOffset = Mathf.Sin((t + Time.time * waveFrequency) * Mathf.PI * 2f) * waveAmplitude;
            Vector3 waveOffsetVector = CalculateWaveOffset(waveOffset, direction);

            particles[i].position = basePosition + waveOffsetVector;

            particles[i].rotation3D = Quaternion.LookRotation(direction).eulerAngles;

            particles[i].startSize3D = Vector3.one;
            particles[i].startColor = Color.white;
        }

        chainParticleSystem.SetParticles(particles, activeParticles);
    }

    private Vector3 CalculateWaveOffset(float waveOffset, Vector3 direction)
    {
        if (mainCamera == null)
            return Vector3.zero;

        Vector3 toCamera = mainCamera.transform.position - (point1.transform.position + point2.transform.position) * 0.5f;
        Vector3 perpendicular = Vector3.Cross(direction, toCamera).normalized;

        return perpendicular * waveOffset;
    }
}
