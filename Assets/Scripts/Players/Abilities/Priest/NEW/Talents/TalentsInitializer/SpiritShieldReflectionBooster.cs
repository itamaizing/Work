using Mirror;
using UnityEngine;

public class SpiritShieldReflectionBooster : SkillTalentHandler
{
    private bool _enabled;
    private const float ReflectionPercent = 0.5f;
    private const float AoERadius = 6f;
    
    public bool Enabled => _enabled;
    public float ReflectionDamagePercent => ReflectionPercent;

    public SpiritShieldReflectionBooster(NetworkBehaviour owner) : base(owner) { }

    public override void Enable(bool value) => _enabled = value;
    
    public bool TryReflectDamage(Character reflector, Damage incomingDamage, Skill sourceSkill)
    {
        if (!_enabled || incomingDamage.Value <= 0f) 
            return false;

        var reflectorState = reflector.CharacterState;
        if (reflectorState == null) return false;

        bool hasSpiritEnergy = reflectorState.CheckForState(States.SpiritEnergy);
        bool hasShield = reflectorState.CheckForState(States.LightShield);
        if (!hasSpiritEnergy || !hasShield) 
            return false;

        Character caster = Owner.GetComponent<Character>();
        if (caster == null) return false;
        return true;
    }

    public void ReflectDamageToAttacker(Damage originalDamage, Skill sourceSkill)
    {
        Character attacker = sourceSkill?.Hero;
        if (attacker == null || attacker.IsDead) return;
        Damage reflectDamage = new Damage
        {
            Value = originalDamage.Value * ReflectionPercent,
            Type = originalDamage.Type,
            School = originalDamage.School,
            Form = originalDamage.Form,
            PhysicAttackType = originalDamage.PhysicAttackType
        };

        attacker.Health.TryTakeDamage(ref reflectDamage, null);
    }

    public void ReflectDamageAoE(Character caster, Damage originalDamage)
    {
        if (!Owner.isOwned) return;

        Collider[] hits = Physics.OverlapSphere(caster.transform.position, AoERadius, LayerMask.GetMask("Enemy"));

        Damage reflectDamage = new Damage
        {
            Value = originalDamage.Value * ReflectionPercent,
            Type = originalDamage.Type,
            School = originalDamage.School
        };

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<Character>(out var enemy)) continue;
            if (enemy.IsDead) continue;

            enemy.Health.CmdTryTakeDamage(reflectDamage, null);
        }
    }
}
