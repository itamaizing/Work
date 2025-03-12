using Mirror;
using UnityEngine;

public class ChainController : NetworkBehaviour
{
    [SerializeField] private LineRenderer _line;

    [SyncVar(hook = nameof(OnPlayerChanged))]
    private uint _playerNetId;

    [SyncVar(hook = nameof(OnTargetChanged))]
    private uint _targetNetId;

    private Transform _playerTransform;
    private Transform _targetTransform;

    public void InitChain(Transform player, Transform target)
    {
        _playerNetId = player.GetComponent<NetworkIdentity>().netId;
        _targetNetId = target.GetComponent<NetworkIdentity>().netId;

        SetTransforms(_playerNetId, _targetNetId);
    }

    private void OnPlayerChanged(uint oldId, uint newId)
    {
        SetTransforms(newId, _targetNetId);
    }

    private void OnTargetChanged(uint oldId, uint newId)
    {
        SetTransforms(_playerNetId, newId);
    }

    private void SetTransforms(uint playerId, uint targetId)
    {
        if (NetworkClient.spawned.TryGetValue(playerId, out var playerIdentity))
            _playerTransform = playerIdentity.transform;

        if (NetworkClient.spawned.TryGetValue(targetId, out var targetIdentity))
            _targetTransform = targetIdentity.transform;

        UpdateLinePositions();
    }

    private void Update()
    {
        if (_playerTransform && _targetTransform)
            UpdateLinePositions();
    }

    private void UpdateLinePositions()
    {
        _line.SetPosition(0, _playerTransform.position + Vector3.up * 1.5f);
        _line.SetPosition(1, _targetTransform.position);
    }
}
