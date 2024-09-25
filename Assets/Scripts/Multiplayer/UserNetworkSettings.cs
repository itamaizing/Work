using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UserNetworkSettings : NetworkBehaviour
{
    private List<HeroComponent> _allies = new List<HeroComponent>();
    private List<HeroComponent> _enemies = new List<HeroComponent>();
    public readonly SyncList<GameObject> Players = new SyncList<GameObject>();


    [SyncVar] private byte _teamIndex;

    private Health _cachedHealth;

    public byte TeamIndex
    {
        get => _teamIndex;
        set
        {
            if (isServer)
            {
                _teamIndex = value;
                TargetUpdateLayers(connectionToClient);
            }
        }
    }

    public Health CachedHealth
    {
        get
        {
            if (_cachedHealth == null)
            {
                _cachedHealth = GetComponent<Health>();
            }
            return _cachedHealth;
        }
    }

    [SyncVar] private Vector3 spawnPosition;

    public Scene MyRoom { get; set; }

    public void SetSpawnPosition(Vector3 position)
    {
        if (isServer)
        {
            spawnPosition = position;
            RpcUpdatePosition(position);
        }
    }

    [ClientRpc]
    private void RpcUpdatePosition(Vector3 position)
    {
        transform.position = position;
    }

    [TargetRpc]
    public void TargetUpdateLayers(NetworkConnection target)
    {
        MarkUpEnemiesOrAllies();
    }

    public void MarkUpEnemiesOrAllies()
    {
        foreach (var item in Players)
        {

            if (item.GetComponent<UserNetworkSettings>().TeamIndex != _teamIndex)
            {
                item.gameObject.layer = LayerMask.NameToLayer("Enemy");
                _enemies.Add(item.GetComponent<HeroComponent>());
            }
            else
            {
                item.gameObject.layer = LayerMask.NameToLayer("Allies");
                _allies.Add(item.GetComponent<HeroComponent>());
            }
        }
    }
}
