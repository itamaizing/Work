using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCall : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Call();
    }

    public void Call()
    {
        Debug.Log(123123123);
    }
}
