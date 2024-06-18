using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectButton : MonoBehaviour
{
    [SerializeField] private MultiplayerManager _manager;
    public void Click()
    {
        _manager.StartClient();
    }
}
