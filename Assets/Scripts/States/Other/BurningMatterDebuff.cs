using System.Collections.Generic;
using UnityEngine;

public class BurningMatterDebuff : RefreshingState
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

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        if (this.damageToExit == 0)
        {
            this.damageToExit = 10000;
        }
        else
        {
            this.damageToExit = damageToExit;
        }
        
        characterState = character;
        MaxStacksCount = 1;
        _baseDuration = durationToExit;
        duration = durationToExit;
        _lastPosition   = characterState.Character.transform.position;
    }

    public override bool Stack(float time)
    {
        duration = _baseDuration;
        return true;
    }

    public override void UpdateState()
    {
        if (duration <= 0)
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
