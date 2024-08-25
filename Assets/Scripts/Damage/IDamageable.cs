using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageable
{
    public event Action<float, DamageType> DamageTaked;
    public bool TryTakeDamage(ref float damage, IDamageDealer skill);
}
