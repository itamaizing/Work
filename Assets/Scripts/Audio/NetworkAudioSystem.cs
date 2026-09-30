using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class NetworkAudioSystem : NetworkBehaviour
{
    private readonly Dictionary<(int clipHash, uint followNetId, int requestId), AudioHandle> _loops = new();

    private void Awake() => AudioManager.RegisterNetwork(this);
    
    private void OnDestroy()
    {
        if (AudioManager.Network == this)
            AudioManager.RegisterNetwork(null);
    }

    #region Public API (безопасно звать откуда угодно)

    public void Play(AudioData data)
    {
        if (NetworkServer.active)
            RpcPlay(data);
        else if (NetworkClient.isConnected)
            CmdPlay(data);
        else
            PlayLocal(data);
    }

    public void TryStop(AudioData data)
    {
        if (NetworkServer.active)
            RpcStop(data);
        else if (NetworkClient.isConnected)
            CmdStop(data);
        else
            StopLocal(data);
    }

    #endregion

    #region Network

    [Command(requiresAuthority = false)]
    private void CmdPlay(AudioData data) => RpcPlay(data);

    [Command(requiresAuthority = false)]
    private void CmdStop(AudioData data) => RpcStop(data);

    [ClientRpc]
    private void RpcPlay(AudioData data) => PlayLocal(data);

    [ClientRpc]
    private void RpcStop(AudioData data) => StopLocal(data);

    #endregion

    #region Local playback (выполняется на каждом клиенте)

    private void PlayLocal(AudioData data)
    {
        var local = AudioManager.Local;
        if (local == null) return;

        Transform follow = null;
        if (data.FollowTargetNetId != 0)
        {
            if (!NetworkClient.spawned.TryGetValue(data.FollowTargetNetId, out var identity) || identity == null)
                return;
            follow = identity.transform;
        }

        Vector3? position = data.HasPosition ? data.Position : (Vector3?)null;

        bool isLoop = data.PlayMode == SfxPlayMode.Loop;
        var key = KeyOf(data);
        
        if (isLoop && _loops.Remove(key, out var old))
            old.Stop();

        var handle = local.Play(data, position, follow);
        if (handle == null || !isLoop) return;

        _loops[key] = handle;
        handle.Released -= OnLoopReleased;
        handle.Released += OnLoopReleased;
    }

    private void StopLocal(AudioData data)
    {
        if (_loops.Remove(KeyOf(data), out var handle))
            handle.Stop();
    }

    private void OnLoopReleased(AudioHandle handle)
    {
        (int, uint, int)? found = null;
        foreach (var kv in _loops)
        {
            if (kv.Value != handle) continue;
            found = (kv.Key.clipHash, kv.Key.followNetId, kv.Key.requestId);
            break;
        }

        if (found.HasValue)
            _loops.Remove(found.Value);
    }

    private static (int, uint, int) KeyOf(AudioData d) => (d.ClipHash, d.FollowTargetNetId, d.RequestId);

    #endregion
}