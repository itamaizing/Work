using System.Collections.Generic;
using UnityEngine;

public class ReversePolarityState : AbstractCharacterState
{
    public override States State => States.ReversePolarity;
    public override StateType Type => StateType.Immaterial;
    public override List<StatusEffect> Effects => new List<StatusEffect>();
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    
    private const float _tickInterval  = 1f;
    private const float _damagePercent = 0.01f;
    private float _tickTimer = 0f;

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _tickTimer = 0f;
    }

    public override void UpdateState()
    {
        if (!characterState.isServer) return;

        _tickTimer += Time.deltaTime;

        if (_tickTimer < _tickInterval) return;

        _tickTimer -= _tickInterval;

        float damageValue = health.MaxValue * _damagePercent;

        Damage damage = new Damage
        {
            Value = damageValue,
            Type  = DamageType.Magical,
        };

        characterState.Character.TryTakeDamage(ref damage, skill);
    }

    public override bool Stack(float time)
    {
        return false;
    }
}
