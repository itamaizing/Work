using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test_AttackSpeedChangedSystem : MonoBehaviour
{
    private Character _character;
    private Skill _skill;

    private float _baseAttackSpeed;
    private float _currentAttackSpeed;
    private float _changedAttackSpeed;

    private bool _isCahngedAttackSpeed = false;
    private bool _isCanChangedAttackSpeed = false;

    public float BaseAttackSpeed { get => _baseAttackSpeed; set => _baseAttackSpeed = value; }
    public float CurrentAttackSpeed { get => _currentAttackSpeed; set => _currentAttackSpeed = value; }
    public float ChangedAttackSpeed { get => _changedAttackSpeed; set => _changedAttackSpeed = value; }

    public bool IsCahngedAttackSpeed { get => _isCahngedAttackSpeed; set => _isCahngedAttackSpeed = value; }
    public bool IsCanChangedAttackSpeed { get => _isCanChangedAttackSpeed; set => _isCanChangedAttackSpeed = value; }

    public Character Character { get => _character; set => _character = value; }

    public void IncreaseAttackSpeed(float value, Skill skill)
    {
        _skill = skill;
    }

    public void ReductionAttackspeed(float value)
    {

    }

    public void ResetToBaseAttackSpeed(float value)
    {

    }

}
