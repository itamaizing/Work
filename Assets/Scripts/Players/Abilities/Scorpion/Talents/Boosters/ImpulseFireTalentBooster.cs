using Mirror;
using UnityEngine;

public class ImpulseFireTalentBooster : SkillTalentHandler
{
    private bool _enabled;

    public ImpulseFireTalentBooster(NetworkBehaviour owner) : base(owner) { }

    public override void Enable(bool value) => _enabled = value;

    public int ApplyTeleportDiscount(int baseCost, Character target)
    {
        if (!_enabled || target == null) return baseCost;

        int stacks = target.CharacterState.CheckStateStacks(States.ScorchedSoul);
        if (stacks <= 0) return baseCost;

        int divisor = stacks + 1;
        return Mathf.CeilToInt(baseCost / (float)divisor);
    }

    public bool CanUseInCombo(Skill skill)
    {
        if (!_enabled)
        {
            return skill.Info.DamageType == DamageType.Physical;
        }
        return skill.Info.DamageType == DamageType.Physical || skill.Info.DamageType == DamageType.Magical;
    }
}