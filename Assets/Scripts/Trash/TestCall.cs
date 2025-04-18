using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCall : MonoBehaviour, IDamageable
{
    public event Action<Damage, Skill> DamageTaken;

    public void ShowPhantomValue(Damage phantomValue)
    {
        throw new NotImplementedException();
    }

    public bool TryTakeDamage(ref Damage damage, Skill skill)
    {
        throw new NotImplementedException();
    }

    public void Call(int i)
    {
        Debug.Log(i);
    }
}
