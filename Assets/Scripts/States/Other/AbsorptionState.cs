using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbsorptionState : StackableState, IDamageable
{
    private float _damageAbsorbed;
    private float _maxAbsorption;
    private float _curentAbsorption;

    public event Action<Damage, Skill> DamageTaken;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State => States.Absorption;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => new List<StatusEffect>();

    public Transform transform => throw new NotImplementedException();
    public GameObject gameObject => throw new NotImplementedException();

    protected override void OnEnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _maxAbsorption = damageToExit;
        _curentAbsorption = _maxAbsorption;
        _damageAbsorbed = 0;
        characterState.Character.Health.TotalMaxAbsorption += _maxAbsorption;
        characterState.Character.Health.AddShieldValues(_maxAbsorption);

        UpdateShieldValues();
    }

    public override void OnUpdateState()
    {
    }

    protected override void OnExitState()
    {
        ResetCharacterShieldValues();
    }

    public override bool Stack(float time)
    {
        //_duration = time;
        //_damageAbsorbed = 0;
        currentStacksCount += 1;
        return false;
    }

    public bool TryTakeDamage(ref Damage damage, Skill skill)
    {
        Debug.Log($"Урон по стостоянию: {_damageAbsorbed}");
        float damageToAbsorb = Mathf.Min(characterState.Character.Health.TotalMaxAbsorption - _damageAbsorbed, damage.Value);
        _damageAbsorbed += damageToAbsorb;
        damage.Value -= damageToAbsorb;
        _curentAbsorption = _maxAbsorption - _damageAbsorbed;

        var tempDamage = new Damage
        {
            Form = damage.Form,
            PhysicAttackType = damage.PhysicAttackType,
            School = damage.School,
            Type = damage.Type,
            Value = damageToAbsorb,
        };

        characterState.GetComponent<Character>().DamageTracker.AddDamage(damage, null);
        DamageTaken?.Invoke(tempDamage, skill);

        characterState.Character.Health.ClientRpcInvokeShieldDamageTaken(damageToAbsorb, damage.Type, skill);

        UpdateShieldValues();

        if (_damageAbsorbed >= _maxAbsorption)
        {
            ExitState();
            return true;
        }

        return damage.Value == 0;
    }

    public void UpdateShieldValues()
    {
        if (characterState.Character.Health != null)
        {
            characterState.Character.Health.UpdateShieldValues(_damageAbsorbed, characterState.Character.Health.TotalMaxAbsorption);
        }
    }

    private void ResetCharacterShieldValues()
    {
        characterState.Character.Health.TotalMaxAbsorption -= _curentAbsorption + _damageAbsorbed;
        characterState.Character.Health.UpdateShieldValues(0, characterState.Character.Health.TotalMaxAbsorption);
        if (characterState.Character.Health.TotalMaxAbsorption <= 0) characterState.Character.Health.ResetShieldValues();
        characterState.Character.Health.AddShieldValues(-_curentAbsorption);
    }

    public void ShowPhantomValue(Damage phantomValue)
    {
        
    }
}