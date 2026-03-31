using Mirror;
using UnityEngine;

public class LightShieldManaRestoreBooster : SkillTalentHandler
{
    private bool _enabled;
    public bool Enabled => _enabled;

    public LightShieldManaRestoreBooster(NetworkBehaviour owner) : base(owner)
    {
    }

    public override void Enable(bool value) => _enabled = value;
    
    public void OnShieldAbsorbedDamage(Character shieldOwner, float absorbedAmount)
    {
        if (!_enabled || absorbedAmount <= 0f)
            return;
        var manaResource = shieldOwner.TryGetResource(ResourceType.Mana);
        if (manaResource != null)
        {
            manaResource.Add(absorbedAmount);
        }
    }
}