using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class LightShield : StackableState, IDamageable
{
    private BladeMailPriestTalent _bladeMailPriestTalent;
    private GameObject _lightShield;

    private float _damageAbsorbed;
    private float _maxAbsorption;
    private float _duration;

    public event Action<Damage, Skill> DamageTaken;

    public override States State => States.LightShield;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => new List<StatusEffect>();
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;

    public Transform transform => throw new NotImplementedException();
    public GameObject gameObject => throw new NotImplementedException();

    protected override void EnterState(CharacterState character, float durationToExit, float maxDamageAbsorbed, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        _duration = durationToExit;
        _damageAbsorbed = 0;
        _maxAbsorption = maxDamageAbsorbed;
        base.personWhoMadeBuff = personWhoMadeBuff;

        if (characterState.StateEffects.LightShield != null)
        {
            _lightShield = characterState.StateEffects.LightShield;
            _lightShield.SetActive(true);
        }

        if (characterState.TryGetComponent<Health>(out var health))
        {
            health.AddShieldValues(_maxAbsorption);
            health.UpdateShieldValues(_damageAbsorbed, _maxAbsorption);
        }
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;

        if (_duration <= 0 || _damageAbsorbed >= _maxAbsorption)
        {
            GlobalExit();
        }
    }

    protected override void ExitState()
    {
        if (characterState.TryGetComponent<Health>(out var health))
        {
            health.ResetShieldValues();
        }

        characterState.RemoveStateFromList(this);

        if (_lightShield != null)
            _lightShield.SetActive(false);
    }

    public override bool Stack(float time)
    {
        _duration = time;
        _damageAbsorbed = 0;

        if (characterState.TryGetComponent<Health>(out var health))
        {
            health.AddShieldValues(_maxAbsorption);
            health.UpdateShieldValues(_damageAbsorbed, _maxAbsorption);
        }

        return false;
    }

    public bool TryTakeDamage(ref Damage damage, Skill skill)
    {
        if (_damageAbsorbed >= _maxAbsorption)
        {
            GlobalExit();
            return false;
        }

        float damageToAbsorb = Mathf.Min(_maxAbsorption - _damageAbsorbed, damage.Value);

        _damageAbsorbed += damageToAbsorb;
        damage.Value -= damageToAbsorb;

        Debug.LogError("damage absorbed: " + _damageAbsorbed);
        
        if (characterState.TryGetComponent<Health>(out var health))
        {
            health.UpdateShieldValues(_damageAbsorbed, _maxAbsorption);
        }
        
        if (damageToAbsorb > 0)
        {
            var pShield = personWhoMadeBuff?.Abilities?.GetSkill<PriestShield>();
            pShield?.LightShieldManaRestoreBooster?.OnShieldAbsorbedDamage(characterState.Character, damageToAbsorb);
        }

        if (damageToAbsorb > 0)
        {
            var pShield = personWhoMadeBuff?.Abilities?.GetSkill<PriestShield>();
            if (pShield != null)
            {
                pShield.TryApplyTalents(characterState.Character, 
                    new Damage 
                    { 
                        Value = damageToAbsorb, 
                        School = damage.School, 
                        Type = damage.Type 
                    }, 
                    skill);
            }
        }

        if (_damageAbsorbed >= _maxAbsorption)
        {
            GlobalExit();
            return true;
        }

        return damage.Value == 0;
    }


    public void ShowPhantomValue(Damage phantomValue)
    {
        
    }
}