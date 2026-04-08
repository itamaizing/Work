using System;
using Unity.VisualScripting;
using UnityEngine;

public interface IComboParticipatingSkill
{
    public event Action<GameObject, Skill> OnDamaged;
}
