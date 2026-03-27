using Mirror;
using UnityEngine;

public class RestorationHealBooster : SkillTalentHandler
{
    private bool _enabled;
    private float _bonusHeal = 0f;

    public bool Enabled => _enabled;
    public float BonusHeal => _bonusHeal;

    public RestorationHealBooster(NetworkBehaviour owner) : base(owner) { }

    public void Enable(bool value)
    {
        if (_enabled == value) return;
        _enabled = value;
        if (!_enabled) Reset();
    }

    public void OnHealReceived(float healAmount)
    {
        if (!_enabled) return;
        if (healAmount <= 0f) return;

        _bonusHeal += healAmount * 0.2f;
    }

    public void Reset() => _bonusHeal = 0f;
}
