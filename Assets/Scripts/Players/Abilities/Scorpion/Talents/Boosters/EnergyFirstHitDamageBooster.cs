using Mirror;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class EnergyFirstHitDamageBooster : Skill,IPassiveSkill
{
    private float _thresholdEnergy = 80f;
    private float _damagePercentPer2Energy = 1f;
    
    private Resource _energyResource;

    private Character _lastTarget;
    private bool _isEnabled = false;

    public bool IsEnabled => _isEnabled;

    public void EnableBooster(bool value)
    {
        if (_isEnabled == value) return;
        _isEnabled = value;
        
        CmdEnable(_isEnabled);
    }

    [Command]
    private void CmdEnable(bool value)
    {
        if (_energyResource == null)
        {
            _energyResource = _hero.TryGetResource(ResourceType.Energy);
        }
        _isEnabled = value;
        if (value)
            SubscribeToAllPhysicalSkills();
        else
            UnsubscribeFromAllPhysicalSkills();
    }
    
    private void SubscribeToAllPhysicalSkills()
    {
        foreach (var skill in _hero.Abilities.Abilities)
        {
            if (skill.Info.AbilityForm != AbilityForm.Physical && skill.Info.AbilityForm != AbilityForm.Both)
                continue;

            if (skill is IComboParticipatingSkill)
            {
                (skill as IComboParticipatingSkill).OnBeforeApplyParticipatingDamage += OnBeforeDamageApplied;
            }
            else
            {
                skill.OnBeforeApplyDamage += OnBeforeDamageApplied;
            }
        }
    }
    
    private void UnsubscribeFromAllPhysicalSkills()
    {
        foreach (var skill in _hero.Abilities.Abilities)
        {
            if (skill.Info.AbilityForm != AbilityForm.Physical && skill.Info.AbilityForm != AbilityForm.Both)
                continue;

            if (skill is IComboParticipatingSkill)
            {
                (skill as IComboParticipatingSkill).OnBeforeApplyParticipatingDamage -= OnBeforeDamageApplied;
            }
            else
            {
                skill.OnBeforeApplyDamage -= OnBeforeDamageApplied;
            }
        }
    }

    private void OnBeforeDamageApplied(ref Damage damage, Skill skill, GameObject targetGo)
    {
        if (!_isEnabled || _energyResource == null) return;
        if (targetGo == null) return;
        if (!targetGo.TryGetComponent<Character>(out Character target)) return;
        if (target == _lastTarget) return;
        _lastTarget = target;

        float currentEnergy = _energyResource.CurrentValue;
        if (currentEnergy <= _thresholdEnergy) return;

        float bonusPercent = (Mathf.Floor((currentEnergy - _thresholdEnergy) / 2f) * _damagePercentPer2Energy) / 100f;

        if (bonusPercent > 0f)
        {
            float extraValue = damage.Value * bonusPercent;

            Damage extraDamage = new Damage { Value = extraValue, Type = damage.Type, School = Schools.Physical };
            skill.ApplyDamage(extraDamage, targetGo);
        }
    }

    protected override IEnumerator CastJob()
    {
        throw new NotImplementedException();
    }

    protected override int AnimTriggerCastDelay { get; }
    protected override int AnimTriggerCast { get; }
}