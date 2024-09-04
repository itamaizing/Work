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
        if (Instance != null)
            Debug.LogError("2 Users on client?!");

        Instance = this;
    }
}
