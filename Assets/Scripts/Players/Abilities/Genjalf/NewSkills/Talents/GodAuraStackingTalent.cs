using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GodAuraStackingTalent : Talent
{
    [SerializeField] private float _stackChance = 30f;
    
    private bool _isProcessing = false;
    private bool _isSubscribed = false;

    public override void Enter()
    {
        if (_isSubscribed) return;
        character.Health.DamageTaken += OnDamageTaken;
        _isSubscribed = true;
    }

    public override void Exit()
    {
        if (!_isSubscribed) return;
        character.Health.DamageTaken -= OnDamageTaken;
        _isSubscribed = false;
    }

    private void OnDamageTaken(Damage damage, Skill skill)
    {
        if (skill == null) return;
        if (_isProcessing) return;
        if (Random.Range(0f, 100f) > _stackChance) return;

        var godAura = character.GetComponent<GodAura>();
        if (godAura == null && !godAura.IsActive) return;

        _isProcessing = true;

        godAura.AddTalentStack();

        _isProcessing = false;
    }
}
