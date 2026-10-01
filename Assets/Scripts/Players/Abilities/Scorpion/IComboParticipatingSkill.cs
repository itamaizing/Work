using System;
using Unity.VisualScripting;
using UnityEngine;

public interface IComboParticipatingSkill
{
    public delegate void OnBeforeApplyDamageDelegate(ref Damage damage, Skill skill,GameObject target);
    public event OnBeforeApplyDamageDelegate OnBeforeApplyParticipatingDamage;
    public event Action<GameObject, Skill> OnDamaged;
    
    public void OnFinalComboSkill(GameObject target);

    public void OnTargetHasComboPoint(GameObject target,float comboPoints);
}
