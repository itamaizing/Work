using Mirror;
using UnityEngine;

public class OverhealManaBooster : SkillTalentHandler
{
    private bool _enabled;
    private readonly Character _hero;

    public OverhealManaBooster(NetworkBehaviour owner, Character hero) : base(owner)
    {
        _hero = hero;
    }

    public override void Enable(bool value)
    {
        if (_enabled == value) return;
        _enabled = value;
    }

    public void OnAnyHealTaken(Character target, float healAmount, Skill skill)
    {
        if (!_enabled || !Owner.isOwned || _hero == null || healAmount <= 0f) 
            return;

        if (skill == null || skill.Hero != _hero) 
            return;

        var health = target.Health;
        if (health == null) return;

        float hpBefore = health.CurrentValue;
        if (hpBefore >= health.MaxValue) return;

        float actualHeal = Mathf.Max(0f, health.MaxValue - hpBefore);
        float overheal = healAmount - actualHeal;
        
        if (actualHeal <= 0f || overheal <= 0f) return;

        float manaToRestore = overheal * 2f;

        if (_hero.TryGetResource(ResourceType.Mana) is Resource manaResource)
        {
            manaResource.CmdAdd(manaToRestore);
        }
    }
}
