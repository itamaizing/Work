using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test_OnTrigger : NetworkBehaviour
{
    [Server]
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Collider name = " + other.name);
    }
}
