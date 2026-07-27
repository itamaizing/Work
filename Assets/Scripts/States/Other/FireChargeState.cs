using System.Collections.Generic;
using UnityEngine;

public class FireChargeState : AbstractCharacterState
{
    public override States State => States.FireCharge;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => new List<StatusEffect> { StatusEffect.Strengthening };

    private float _punchKickDamagePercent;
    private float _punchKickScorchedChance;
    private float _bladeDamagePercent;
    private float _bladeScorchedChance;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit,
        Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        health = character.Character.Health;
        abilities = character.Character.Abilities;
        this.personWhoMadeBuff = personWhoMadeBuff;
        duration = durationToExit;

        if (this.damageToExit == 0)
        {
            this.damageToExit = 10000;
        }
        else
        {
            this.damageToExit = damageToExit;
        }
        this.personWhoMadeBuff = personWhoMadeBuff;

        _punchKickDamagePercent = 1f;
        _punchKickScorchedChance = 50f;
        _bladeDamagePercent = 0.5f;
        _bladeScorchedChance = 25f;
    }

    public void ConsumeForPunchKick(NewPunch_Scorpion punch = null, Kick_Scorpion kick = null)
    {
        punch?.AddFireBonus(_punchKickDamagePercent, _punchKickScorchedChance);
        kick?.AddFireBonus(_punchKickDamagePercent, _punchKickScorchedChance);
        ExitState();
    }

    public void ConsumeForBlades(CleavingBlade_Scorpion blade = null, ChainBlade chainBlade = null)
    {
        blade?.AddFireBonus(_bladeDamagePercent, _bladeScorchedChance);
        chainBlade?.AddFireBonus(_bladeDamagePercent, _bladeScorchedChance);
        ExitState();
    }
    
    public override AbstractCharacterState TryApply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        if (!CanEnterState(character)) return null;
        
        EnterState(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);

        return this;
    }

    public override void UpdateState() { }

    public override void ExitState()
    {
        _punchKickDamagePercent = 0f;
        _punchKickScorchedChance = 0f;
        _bladeDamagePercent = 0f;
        _bladeScorchedChance = 0f;
        characterState?.RemoveState(this);
    }
}