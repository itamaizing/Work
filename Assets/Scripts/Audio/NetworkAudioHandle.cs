using System;
using Mirror;
using UnityEngine;

public class NetworkAudioHandle : NetworkBehaviour
{
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private SoundDatabase _database;

    public bool IsInUse { get; private set; }
    public event Action<NetworkAudioHandle> Released;

    private uint _followTargetNetId;

    [ClientRpc]
    public void RpcPlay(AudioData data)
    {
        if (!_database.TryGet(data.ClipHash, out var asset))
        {
            Debug.LogWarning($"[NetworkAudioHandle] Unknown clip hash {data.ClipHash}");
            return;
        }

        IsInUse = true;
        gameObject.SetActive(true);
        AudioHandleUtility.ApplyAudioData(_audioSource, data, asset);

        _followTargetNetId = data.FollowTargetNetId;

        if (_followTargetNetId != 0 && NetworkClient.spawned.TryGetValue(_followTargetNetId, out var identity))
        {
            _audioSource.spatialBlend = 1f;
            transform.position = identity.transform.position;
        }
        else if (data.HasPosition)
        {
            _audioSource.spatialBlend = 1f;
            transform.position = data.Position;
        }
        else
        {
            _audioSource.spatialBlend = 0f;
        }

        _audioSource.Play();
    }

    private void Update()
    {
        if (_followTargetNetId != 0 && NetworkClient.spawned.TryGetValue(_followTargetNetId, out var identity))
            transform.position = identity.transform.position;
    }

    [ClientRpc]
    public void RpcStop()
    {
        _audioSource.Stop();
        IsInUse = false;
        _followTargetNetId = 0;
        gameObject.SetActive(false);
    }

    [Server]
    public void ServerRelease()
    {
        IsInUse = false;
        Released?.Invoke(this);
    }
}