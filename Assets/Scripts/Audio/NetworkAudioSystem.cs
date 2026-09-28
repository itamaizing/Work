using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class NetworkAudioSystem : NetworkBehaviour
{
    [SerializeField] private NetworkAudioHandle _handlePrefab;
    [SerializeField] private SoundDatabase _database;
    [SerializeField] private int _perPlayerPoolSize = 10;

    private readonly List<NetworkAudioHandle> _pool = new();
    private readonly Dictionary<(int clipHash, uint followNetId, int requestId), NetworkAudioHandle> _active = new();

    private void Awake() => AudioManager.RegisterNetwork(this);

    public override void OnStartServer()
    {
        base.OnStartServer();

        var gameRules = GetComponent<GameRules>();
        int capacity = gameRules != null ? gameRules.MaxPlayers : 2;

        ServerEnsurePoolSize(capacity);
    }
    
    [Server]
    public void ServerEnsurePoolSize(int playerCount)
    {
        int target = Mathf.Max(1, playerCount) * _perPlayerPoolSize;
        while (_pool.Count < target)
            _pool.Add(SpawnHandle());
    }

    private NetworkAudioHandle SpawnHandle()
    {
        var handle = Instantiate(_handlePrefab);
        handle.Released += OnHandleReleasedServer;
        NetworkServer.Spawn(handle.gameObject);
        return handle;
    }

    private void OnHandleReleasedServer(NetworkAudioHandle handle)
    {
        var key = _active.FirstOrDefault(kv => kv.Value == handle).Key;
        _active.Remove(key);
    }

    public void Play(AudioData data) => CmdPlay(data);

    [Command(requiresAuthority = false)]
    private void CmdPlay(AudioData data)
    {
        if (!_database.TryGet(data.ClipHash, out var asset))
        {
            Debug.LogWarning($"[NetworkAudioSystem] Unknown clip hash {data.ClipHash}");
            return;
        }

        var handle = _pool.FirstOrDefault(h => !h.IsInUse) ?? SpawnHandle();

        var key = (data.ClipHash, data.FollowTargetNetId, data.RequestId);
        _active[key] = handle;

        handle.RpcPlay(data);

        if (data.PlayMode == SfxPlayMode.Once)
            StartCoroutine(AutoReleaseAfter(handle, asset.Clip.length));
    }

    private IEnumerator AutoReleaseAfter(NetworkAudioHandle handle, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (handle.IsInUse)
        {
            handle.RpcStop();
            handle.ServerRelease();
        }
    }

    public void TryStop(AudioData data) => CmdStop(data);

    [Command(requiresAuthority = false)]
    private void CmdStop(AudioData data)
    {
        var key = (data.ClipHash, data.FollowTargetNetId, data.RequestId);
        if (_active.TryGetValue(key, out var handle))
        {
            handle.RpcStop();
            handle.ServerRelease();
        }
    }
}