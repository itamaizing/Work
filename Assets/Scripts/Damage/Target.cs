using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class Target : NetworkBehaviour
{
    public Collider Collider;
    public List<Collider> Colors;

    private void OnTriggerEnter(Collider other)
    {
        Collider = other;
        Colors.Add(Collider);
    }

    public void Test()
    {
        Collider = null;
        Debug.Log(Colors[0]);
    }

    private void Call(int i)
    {
        Debug.Log(i);
    }
}
