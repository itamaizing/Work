using Mirror;
using UnityEngine;

public class ImpulseFireTalentBooster : SkillTalentHandler
{
    private bool _enabled;

    public ImpulseFireTalentBooster(NetworkBehaviour owner) : base(owner) { }

    public override void Enable(bool value) => _enabled = value;

    public bool CanUseInCombo(Skill skill)
    {
        if (!_enabled)
        {
            return skill.Info.DamageType == DamageType.Physical;
        }
        return skill.Info.DamageType == DamageType.Physical || skill.Info.DamageType == DamageType.Magical;
    }
}