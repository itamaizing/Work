using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UserNetworkSettings : NetworkBehaviour
{
    private List<HeroComponent> _allies = new List<HeroComponent>();
    private List<HeroComponent> _enemies = new List<HeroComponent>();
    public readonly SyncList<GameObject> Players = new SyncList<GameObject>();

    private Scene myRoom;

    [SyncVar] private byte _teamIndex;

    public int playerNumber;
    public int scoreIndex;
    public int matchIndex;
    public uint score;
    public int clientMatchIndex = -1;

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

    public Scene MyRoom { get => myRoom; set => myRoom = value; }

    [SyncVar] private Vector3 spawnPosition;

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
        _allies.Clear();
        _enemies.Clear();

        foreach (var item in FindObjectsOfType<UserNetworkSettings>())
        {
            if (item.TeamIndex != _teamIndex)
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