using Mirror;
using System;
using System.Linq;
using UnityEngine;

public class EnergyFirstHitDamageBooster : SkillTalentHandler
{
    [Header("Settings")]
    [SerializeField] private float _thresholdEnergy = 80f;
    [SerializeField] private float _damagePercentPer2Energy = 1f;

    private Character _hero;
    private Resource _energyResource;

    private Character _lastTarget;
    private bool _isEnabled = false;

    public bool IsEnabled => _isEnabled;

    public EnergyFirstHitDamageBooster(NetworkBehaviour owner) : base(owner)
    {
        _hero = owner as Character;
        if (_hero != null)
            _energyResource = _hero.TryGetResource(ResourceType.Energy);
    }

    public override void Enable(bool value)
    {
        if(_isEnabled == value) return;
        _isEnabled = value;

        if (value)
            SubscribeToAllPhysicalSkills();
        else
            UnsubscribeFromAllPhysicalSkills();
    }

    private void SubscribeToAllPhysicalSkills()
    {
        var abilities = _hero.Abilities.Abilities;

        foreach (var skill in abilities)
        {
            if (skill.Info.AbilityForm != AbilityForm.Physical && skill.Info.AbilityForm != AbilityForm.Both)
                continue;
            skill.CastSuccess += () => OnPhysicalSkillCastStarted(skill);
        }
    }

    private void UnsubscribeFromAllPhysicalSkills()
    {
        var abilities = _hero.Abilities.Abilities;

        foreach (var skill in abilities)
        {
            if (skill.Info.AbilityForm != AbilityForm.Physical && skill.Info.AbilityForm != AbilityForm.Both)
                continue;

            skill.CastSuccess -= () => OnPhysicalSkillCastStarted(skill);
        }
    }

    private void OnPhysicalSkillCastStarted(Skill skill)
    {
        if (!_isEnabled || _energyResource == null) return;

        var target = skill.Targeting.GetTarget()?.Character;
        if (target == null) return;

        if (target == _lastTarget) return;

        _lastTarget = target;

        float currentEnergy = _energyResource.CurrentValue;

        if (currentEnergy > _thresholdEnergy)
        {
            float bonusPercent = (Mathf.Floor((currentEnergy - _thresholdEnergy) / 2f) * _damagePercentPer2Energy) / 100;

            if (bonusPercent > 0f)
            {
                var dmgValue = skill.Damage;
                var dmg = new Damage{Value = dmgValue * bonusPercent, Type = skill.Info.DamageType };
                if(Owner.isClient)
                    skill.CmdApplyDamage(dmg,target.gameObject);
            }
        }
    }
}