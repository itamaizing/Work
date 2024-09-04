using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class User : NetworkBehaviour
{
    public static User Instance;

    [Client]
    private void Awake()
    {
        Instance = this;
    }
}
