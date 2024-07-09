using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UserNetworkSettings : NetworkBehaviour
{
    [SyncVar]
    private Scene _myRoom;

    [SyncVar]
    private byte _teamIndex;

    [SyncVar]
    public int playerNumber;

    [SyncVar]
    public int scoreIndex;

    [SyncVar]
    public int matchIndex;

    [SyncVar]
    public uint score;

    public int clientMatchIndex = -1;

    public Scene MyRoom { get => _myRoom; set => _myRoom = value; }
    public byte TeamIndex { get => _teamIndex; set => _teamIndex = value; }
}
