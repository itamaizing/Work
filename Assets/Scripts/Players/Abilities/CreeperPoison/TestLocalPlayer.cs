using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestLocalPlayer : NetworkBehaviour
{
    void Start()
    {
        if (isClient)
        {
            Debug.Log("isClient");
        }
        if (isClientOnly)
        {
            Debug.Log("isClientOnly");
        }    
        if (isLocalPlayer)
        {
            Debug.Log("isLocalPlayer");
        }  
    }
}
