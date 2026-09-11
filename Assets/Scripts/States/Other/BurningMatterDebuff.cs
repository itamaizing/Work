using System.Collections.Generic;
using UnityEngine;

public class BurningMatterDebuff : RefreshingStateStacking
{
    private List<StatusEffect> _effects = new List<StatusEffect>();
    protected float _damagePerMetr = 3;
    protected float _baseDamagePerMetr = 3;

    private float _baseDuration;
    private Vector3 _lastPosition;

    public override States State => States.BurningMatter;

    public override StateType Type => StateType.Magic;

    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;

    public override List<StatusEffect> Effects => _effects;

    public override float RemainingDuration => _baseDuration;

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        if (DamageToExit == 0)
        {
            parameters[StateParameter.DamageToExit] = 10000;
        }
        else
        {
            parameters[StateParameter.DamageToExit] = damageToExit;
        }
        
        characterState = character;
        SetMaxStacks(1);
        _baseDuration = durationToExit;
        RemainingDuration = durationToExit;
        _lastPosition   = characterState.Character.transform.position;
    }

    public override bool Stack(float time)
    {
        RemainingDuration = _baseDuration;
        return true;
    }

    public override void UpdateState()
    {
        if (RemainingDuration <= 0)
        {
            ExitState();
        }
        
        if (!characterState.isServer) return;
        
        Vector3 currentPos = characterState.Character.transform.position;
        float distance = Vector3.Distance(_lastPosition, currentPos);

        if (distance > 1f)
        {
            Damage dmg = new Damage
            {
                Value = _damagePerMetr,
                Type = DamageType.Magical,
                School = Schools.Fire
            };
            
            characterState.Character.TryTakeDamage(ref dmg, null);

            _damagePerMetr += _baseDamagePerMetr;
            _lastPosition = currentPos;
        }
    }

    public override void ExitState()
    {
        _damagePerMetr = _baseDamagePerMetr;
        characterState.RemoveState(this);
    }
}
