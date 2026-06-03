using Mirror;
using UnityEngine;

public class ScorchedSoulEnergyRestoreHandler : NetworkBehaviour
{
    private Character _owner;
    private bool _isEnabled = false;

    private const float BaseRestorePercent = 0.20f;
    private const float PerStackBonus = 0.05f;

    public void Initialize(Character owner)
    {
        _owner = owner;
    }

    public void SetActive(bool value)
    {
        if (_isEnabled == value) return;
        _isEnabled = value;

        if (value)
            _owner.Health.DamageTaken += OnDamageTaken;
        else
            _owner.Health.DamageTaken -= OnDamageTaken;
    }

    private void OnDamageTaken(Damage damage, Skill skill)
    {
        if (!_isEnabled) return;
        if (!isOwned) return;
        if (skill == null) return;
        if (damage.Type == DamageType.DOTMag || damage.Type == DamageType.DOTPhys) return;

        var attacker = skill.Hero;
        if (attacker == null) return;

        var scorchedState = attacker.CharacterState.GetState(States.ScorchedSoul);
        if(scorchedState == null) return;

        int scorchedStacks = scorchedState.CurrentStacksCount;
        if (scorchedStacks <= 0) return;

        float restorePercent = BaseRestorePercent + (scorchedStacks - 1) * PerStackBonus;
        float restoreAmount = damage.Value * restorePercent;
        if (_owner.TryGetResource(ResourceType.Energy, out var energy))
        {
            energy.CmdAdd(restoreAmount);
        }
    }

    private void OnDestroy()
    {
        if (_owner != null && _isEnabled)
            _owner.Health.DamageTaken -= OnDamageTaken;
    }
}