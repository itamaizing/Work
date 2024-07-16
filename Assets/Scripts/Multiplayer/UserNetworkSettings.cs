using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UserNetworkSettings : NetworkBehaviour
{
    private List<HeroComponent> _allies = new List<HeroComponent>();
    private List<HeroComponent> _enemies = new List<HeroComponent>();
    public readonly SyncList<GameObject> PlayersGameObject = new SyncList<GameObject>();

    private Scene myRoom;

    [SyncVar] private byte _teamIndex;

    public int playerNumber;
    
    public int scoreIndex;
    
    public int matchIndex;
    
    public uint score;

    public int clientMatchIndex = -1;

    public byte TeamIndex { get => _teamIndex; set => _teamIndex = value; }
    public Scene MyRoom { get => myRoom; set => myRoom = value; }

    [TargetRpc]
    public void MarkUpEnemiesOrAllies()
    {
        Debug.Log(PlayersGameObject.Count);

        foreach (var item in PlayersGameObject)
        {
            Debug.Log(item);
            Debug.Log(item.GetComponent<UserNetworkSettings>());
            Debug.Log(item.GetComponent<UserNetworkSettings>().TeamIndex);
            if(item.GetComponent<UserNetworkSettings>().TeamIndex != _teamIndex)
            {
                item.layer = LayerMask.NameToLayer("Enemy");
                _enemies.Add(item.GetComponent<HeroComponent>());
            }
            else
            {
                item.layer = LayerMask.NameToLayer("Allies");
                _allies.Add(item.GetComponent<HeroComponent>());
            }
        }
    }
}
