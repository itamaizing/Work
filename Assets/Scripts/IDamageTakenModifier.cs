using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageTakenModifier
{
    float ModifyIncomingDamage(Damage damage);
}
