using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UserNetworkSettings : NetworkBehaviour
{
    private Scene myRoom;

    private byte _teamIndex;
    
    public int playerNumber;
    
    public int scoreIndex;
    
    public int matchIndex;
    
    public uint score;

    public int clientMatchIndex = -1;

    public byte TeamIndex { get => _teamIndex; set => _teamIndex = value; }
    public Scene MyRoom { get => myRoom; set => myRoom = value; }
}
