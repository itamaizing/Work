using System;
using Mirror;
using UnityEngine;

public class ExplodingCorpse : SkillCreatureIceDeath
{
    private const float ExplodingDamage  = 20f;
    private const float ExplodingRange   = 1f;
    private const float PlagueDuration   = 5f;

    private bool _isEnabled;

    private Health _health;

    protected override string AnimationTrigger => string.Empty;
    protected override void ApplySkillEffect(Character target) { }

    public void OnCreatureSpawned()
    {
        _isEnabled = true;
    }
    private void OnDestroy()
    {
        if(!_isEnabled) return;
        if (_hero == null) return;
        var colliders = Physics.OverlapSphere(transform.position, ExplodingRange, _targetsLayers);

        foreach (var col in colliders)
        {
            if (!col.TryGetComponent<Character>(out var target)) continue;
            if (target.IsDead) continue;
            Damage damage = new Damage { Value  = ExplodingDamage, Type   = DamageType.Physical, School = Schools.Physical };
            target.Health.CmdTryTakeDamage(damage, null);
            target.CharacterState.CmdAddState(States.Plague, PlagueDuration, 1000f, Schools.Dark, target.gameObject, nameof(ExplodingCorpse));
        }
    }
}
