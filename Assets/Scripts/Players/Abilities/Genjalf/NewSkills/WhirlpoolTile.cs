using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class WhirlpoolTile : NetworkBehaviour
{
    [SyncVar] private float _radius;
    [SyncVar] private float _maxPullForce;
    [SyncVar] private float _minPullForce;
    [SyncVar] private float _tickRate;
    [SyncVar] private byte _ownerTeamIndex;
    [SyncVar] private LayerMask _targetLayers;

    private Coroutine _pullCoroutine;
    private bool _isOwnerClient = false;

    public void Init(byte ownerTeamIndex, LayerMask targetLayers, float radius, float maxPullForce, float minPullForce, float tickRate)
    {
        _ownerTeamIndex = ownerTeamIndex;
        _targetLayers = targetLayers;
        _radius = radius;
        _maxPullForce = maxPullForce;
        _minPullForce = minPullForce;
        _tickRate = tickRate;
    }

    public void StartPull() => RpcStartPull();
    public void StopPull() => RpcStopPull();

    [ClientRpc]
    private void RpcStartPull()
    {
        _pullCoroutine = StartCoroutine(PullCoroutine());
    }

    [ClientRpc]
    private void RpcStopPull()
    {
        if (_pullCoroutine != null)
        {
            StopCoroutine(_pullCoroutine);
            _pullCoroutine = null;
        }
    }

    private IEnumerator PullCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(_tickRate);

            if (!_isOwnerClient) continue;

            Collider[] hits = Physics.OverlapSphere(transform.position, _radius, _targetLayers);

            var targetObjects = new List<GameObject>();
            var targetVelocities = new List<Vector3>();

            foreach (var hit in hits)
            {
                if (!hit.TryGetComponent<Character>(out var character)) continue;
                if (character.NetworkSettings.TeamIndex == _ownerTeamIndex) continue;
                if (character.IsDead) continue;

                float distance = Mathf.Max(Vector3.Distance(transform.position, character.transform.position), 0.1f);

                float t = 1f - Mathf.Clamp01(distance / _radius);
                float force = Mathf.Lerp(_minPullForce, _maxPullForce, t * t);

                Vector3 direction = (transform.position - character.transform.position).normalized;

                Vector3 pullVelocity = direction * force;

                targetObjects.Add(hit.gameObject);
                targetVelocities.Add(pullVelocity);
            }

            if (targetObjects.Count > 0)
                CmdApplyPull(targetObjects.ToArray(), targetVelocities.ToArray());
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdApplyPull(GameObject[] targets, Vector3[] newPositions)
    {
        RpcApplyPull(targets, newPositions);
    }

    [ClientRpc]
    private void RpcApplyPull(GameObject[] targets, Vector3[] newPositions)
    {
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null) continue;

            if (targets[i].TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity = newPositions[i];
            }
            else
            {
                targets[i].transform.position = newPositions[i];
            }
        }
    }

    [TargetRpc]
    public void TargetRpcMarkAsOwner(NetworkConnection conn)
    {
        _isOwnerClient = true;
    }
}
