using System.Collections.Generic;
using UnityEngine;

public class WeakeningSilence : StateStacking, ITickableState
{
    private float _damagePerTick = 3;

    public override States State => States.WeakeningSilence;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => new List<StatusEffect> { StatusEffect.Poison };

    public float TickInterval => 1f;

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character sourceCaster, string skillName)
    {
    }

    public override void Reapply(CharacterState character, float durationToExit, float damageToExit, Character sourceCaster, string skillName)
    {
    }

    public override void UpdateState() { }

    public void Tick()
    {
        ApplyDamage();
    }

    private void ApplyDamage()
    {
        if (characterState == null || characterState.Character == null || !characterState.isServer || _damagePerTick <= 0f)
            return;

        Damage damage = new Damage
        {
            Value = _damagePerTick * CurrentStacksCount,
            Type = DamageType.Magical
        };

        health.TryTakeDamage(ref damage, null);
    }
}