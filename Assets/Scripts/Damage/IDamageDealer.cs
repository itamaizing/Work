using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageDealer
{
    public DamageType DamageType { get; }
    public AttackRangeType AttackRangeType { get; }
}
