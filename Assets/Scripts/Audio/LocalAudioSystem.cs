using System.Collections.Generic;
using UnityEngine;

public class LocalAudioSystem : MonoBehaviour
{
    [SerializeField] private AudioHandle _handlePrefab;
    [SerializeField] private SoundDatabase _database;
    [SerializeField] private AudioSource _oneShotSource;
    [SerializeField] private int _initialPoolSize = 5;

    private readonly List<AudioHandle> _free = new();

    public static LocalAudioSystem Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        for (int i = 0; i < _initialPoolSize; i++)
            _free.Add(CreateHandle());

        AudioManager.RegisterLocal(this);
    }

    private AudioHandle CreateHandle()
    {
        var handle = Instantiate(_handlePrefab, transform);
        handle.gameObject.SetActive(false);
        handle.Released += OnHandleReleased;
        return handle;
    }

    private void OnHandleReleased(AudioHandle handle) => _free.Add(handle);

    public void PlayOneShot(AudioData data)
    {
        if (!_database.TryGet(data.ClipHash, out var asset))
        {
            Debug.LogWarning($"[LocalAudioSystem] Unknown clip hash {data.ClipHash}");
            return;
        }

        float volume = asset.BaseVolume + data.Volume;
        if (data.VolumeDelta > 0f) volume += Random.Range(-data.VolumeDelta, data.VolumeDelta);

        _oneShotSource.pitch = data.PitchDelta > 0f ? 1f + Random.Range(-data.PitchDelta, data.PitchDelta) : 1f;
        _oneShotSource.PlayOneShot(asset.Clip, Mathf.Clamp01(volume));
    }

    public AudioHandle Play(AudioData data, Vector3? position = null, Transform followTarget = null)
    {
        if (!_database.TryGet(data.ClipHash, out var asset))
        {
            Debug.LogWarning($"[LocalAudioSystem] Unknown clip hash {data.ClipHash}");
            return null;
        }

        AudioHandle handle;
        if (_free.Count > 0)
        {
            handle = _free[^1];
            _free.RemoveAt(_free.Count - 1);
        }
        else
        {
            handle = CreateHandle();
        }

        handle.Play(data, asset, position, followTarget);
        return handle;
    }
}