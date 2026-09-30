using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class MucusVisual : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private MucusArea _area;
    [SerializeField] private Renderer _decal;
    [SerializeField] private GameObject _spikePrefab;
    [SerializeField] private Transform _spikesParent;

    [Header("Pool Settings")]
    [SerializeField] private int _initialPoolSize = 5;
    [SerializeField] private float _spikeLifetime = 1.5f;

    private static readonly int RadiusId = Shader.PropertyToID("_Radius");
    private static readonly int SpikeHash = Animator.StringToHash("SpawnSpike");

    private MaterialPropertyBlock _mpb;
    private long _lastPulse = -1;

    private readonly List<SpikeInstance> _spikePool = new();

    private class SpikeInstance
    {
        public GameObject GameObject;
        public Transform Transform;
        public Animator Animator;
        public bool IsInUse;
        public Coroutine HideCoroutine;
    }

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        InitializePool();
    }

    private void Start()
    {
        if (_decal != null && _area != null)
        {
            float size = _area.MaxRadius * 2f;
            _decal.transform.localScale = new Vector3(size, _decal.transform.localScale.y, size);
        }
    }
    
    private void OnEnable()
    {
        if (_area != null) _area.OnSpikePulseTriggered += HandleSpikePulse;
    }

    private void OnDisable()
    {
        if (_area != null) _area.OnSpikePulseTriggered -= HandleSpikePulse;
    }
    
    private void HandleSpikePulse(GameObject[] targets)
    {
        foreach (GameObject targetGo in targets)
        {
            if (targetGo == null) continue;

            SpikeInstance spike = GetPooledSpike();
            spike.Transform.position = targetGo.transform.position;
            spike.Transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            spike.GameObject.SetActive(true);

            if (spike.Animator != null)
            {
                spike.Animator.SetTrigger(SpikeHash);
            }

            if (spike.HideCoroutine != null) StopCoroutine(spike.HideCoroutine);
            spike.HideCoroutine = StartCoroutine(HideSpikeRoutine(spike, _spikeLifetime));
        }
    }

    private void InitializePool()
    {
        Transform parent = _spikesParent != null ? _spikesParent : transform;

        for (int i = 0; i < _initialPoolSize; i++)
        {
            CreateNewSpikeInPool(parent);
        }
    }

    private SpikeInstance CreateNewSpikeInPool(Transform parent)
    {
        GameObject go = Instantiate(_spikePrefab, parent);
        go.SetActive(false);

        SpikeInstance instance = new SpikeInstance
        {
            GameObject = go,
            Transform = go.transform,
            Animator = go.GetComponent<Animator>(),
            IsInUse = false
        };

        _spikePool.Add(instance);
        return instance;
    }

    private SpikeInstance GetPooledSpike()
    {
        for (int i = 0; i < _spikePool.Count; i++)
        {
            if (!_spikePool[i].IsInUse)
            {
                _spikePool[i].IsInUse = true;
                return _spikePool[i];
            }
        }

        Transform parent = _spikesParent != null ? _spikesParent : transform;
        SpikeInstance newSpike = CreateNewSpikeInPool(parent);
        newSpike.IsInUse = true;
        return newSpike;
    }

    private void Update()
    {
        float radius = _area.Radius;

        _decal.GetPropertyBlock(_mpb);
        _mpb.SetFloat(RadiusId, radius);
        _decal.SetPropertyBlock(_mpb);
        _decal.enabled = radius > 0.01f;
    }

    private IEnumerator HideSpikeRoutine(SpikeInstance spike, float delay)
    {
        yield return new WaitForSeconds(delay);

        spike.GameObject.SetActive(false);
        spike.IsInUse = false;
        spike.HideCoroutine = null;
    }
}