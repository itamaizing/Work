using System;
using Unity.VisualScripting;
using UnityEngine;

public interface IComboParticipatingSkill
{
    public event Action<GameObject, Skill> OnDamaged;
    
    public void OnFinalComboSkill(GameObject target);

    public void OnTargetHasComboPoint(GameObject target,float comboPoints);
}
