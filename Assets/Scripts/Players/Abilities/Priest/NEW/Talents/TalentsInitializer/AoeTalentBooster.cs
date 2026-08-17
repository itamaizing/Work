using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class AoeTalentBooster : SkillTalentHandler
{
    private bool _enabled;

    private const float AoeRadius = 1f;
    private const float AoeDamagePercent = 0.3f;
    private const float AoeHealPercent = 0.3f;

    public AoeTalentBooster(NetworkBehaviour owner) : base(owner) { }

    public void Enable(bool value) => _enabled = value;

    public bool IsActive => _enabled;

    public Dictionary<GameObject,Damage> GetDamagebleTarget(Character mainTarget, float mainDamageValue, Skill skill)
    {
        if (!_enabled || mainTarget == null) new Dictionary<GameObject, Damage>();

        float aoeDamage = mainDamageValue * AoeDamagePercent;

        Dictionary<GameObject,Damage> targets = new();
        
        Collider[] hits = Physics.OverlapSphere(mainTarget.transform.position, AoeRadius, LayerMask.GetMask("Enemy"));

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<Character>(out var target)) continue;
            if (target == mainTarget || target.IsDead) continue;

            Damage damage = new Damage
            {
                Value = aoeDamage,
                Type = skill.Info.DamageType,
                School = skill.Info.School
            };

            targets.Add(target.gameObject,damage);
            
            skill.CmdApplyDamage(damage,target.gameObject);
        }

        return targets;
    }

    public Dictionary<GameObject,Heal> GetHealableTargets(Character mainTarget, float mainHealValue, Skill skill)
    {
        if (!_enabled || mainTarget == null) return new Dictionary<GameObject, Heal>();

        float aoeHeal = mainHealValue * AoeHealPercent;

        Dictionary<GameObject,Heal> targets = new();
        
        Collider[] hits = Physics.OverlapSphere(mainTarget.transform.position, AoeRadius, LayerMask.GetMask("Allies"));

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<Character>(out var target)) continue;
            if (target == mainTarget || target.IsDead) continue;

            Heal heal = new Heal { Value = aoeHeal, DamageableSkill = skill };

            targets.Add(target.gameObject,heal);
            
            skill.CmdApplyHeal(heal,target.gameObject,skill,nameof(skill));
        }

        return targets;
    }
}
