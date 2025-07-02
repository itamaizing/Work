using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapStateLife : MonoBehaviour
{
    private Bound owner;

    public void Init(Bound bound) => owner = bound;

    private void OnDestroy() 
    {
        if (owner != null) owner.NotifyTrapDestroyed();
    }
}
