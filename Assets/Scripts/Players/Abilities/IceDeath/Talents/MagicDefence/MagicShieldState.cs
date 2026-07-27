using System.Collections.Generic;
using UnityEngine;

public class MagicShieldState : AbstractCharacterState
{
    private float _durability;
    private bool _isEnemyMode;
    private bool _isZoneMode;

    private MagicDomeZone _magicDomeZone;
    
    public override States State => States.MagicShield;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => _isEnemyMode ? BaffDebaff.Debaff : BaffDebaff.Baff;
    public override Schools Schools => Schools.Dark;

    private static readonly List<StatusEffect> _effects = new();
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit,
        float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        duration = durationToExit;
        _durability = damageToExit;
        _isEnemyMode = skillName.Contains("_enemy");
        _isZoneMode = skillName.Contains("_zone");
        if (_isZoneMode)
            _magicDomeZone = personWhoMadeBuff.GetComponent<MagicDefenceSkill>().TempZone;
        
        if(_isEnemyMode)
            characterState.SetSuppressStateBuffEffects(true);
        else
            characterState.SetSuppressStateDebuffEffects(true);

        if (characterState.Character?.Health != null)
        {
            characterState.Character.Health.OnBeforeHeal += AbsorbMagicHeal;
            characterState.Character.Health.OnBeforeDamage += AbsorbMagicDamage;
        }
    }

    private void AbsorbMagicDamage(ref Damage dmg, Skill skill)
    {
        if (_isEnemyMode) return;
        if (dmg.Form == AbilityForm.Magic || dmg.DamageKey == "State")
        {
            if(!_isZoneMode)
                Consume(dmg.Value);
            else
            {
                _magicDomeZone?.DecreaseDurability(dmg.Value);
            }

            dmg.Value = 0;
        }
    }

    private void AbsorbMagicHeal(ref Heal heal, Skill skill)
    {
        if (!_isEnemyMode) return;
        if(!_isZoneMode)
            Consume(heal.Value);
        else
            _magicDomeZone?.DecreaseDurability(heal.Value);
        heal.Value = 0;
    }

    private float Consume(float value)
    {
        if (value <= _durability)
        {
            _durability -= value;
            if (_durability <= 0f) ExitState();
            return 0f;
        }

        float overflow = value - _durability;
        _durability = 0f;
        ExitState();
        return overflow;
    }
    
    public override bool Stack(float time)
    {
        duration = time;
        return true;
    }

    public override void UpdateState()
    {
    }

    public override void ExitState()
    {
        if (characterState.Character?.Health != null)
        {
            characterState.Character.Health.OnBeforeHeal -= AbsorbMagicHeal;
            characterState.Character.Health.OnBeforeDamage -= AbsorbMagicDamage;
        }
        characterState.SetSuppressStateDebuffEffects(false);
        characterState.SetSuppressStateBuffEffects(false);
        _durability = 0;
        base.ExitState();
    }
}
