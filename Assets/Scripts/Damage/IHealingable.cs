using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IHealingable
{
    public event Action<float> HealTaked;

    public void Heal(float value);
}
