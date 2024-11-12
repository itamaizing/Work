using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class User : NetworkBehaviour
{
    public static User Instance;

    [Client]
    private void Start()
    {
        if (Instance == null && isOwned)
            Instance = this;
    }

    public override void OnStartClient()
    {
        if (!isLocalPlayer)
        {
            return;
        }
    }
}