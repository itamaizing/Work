using System.Collections.Generic;
using UnityEngine;

public class EmeraldSkinState : AbstractCharacterState
{
    //private float _buffDuration = 2f;
    private float _defenseIncrease = 0.9f;
    private float _physDefenseIncrease = 0f;
    private float _magDefenseIncrease = 0f;

    private float _flashBuffDuration = 1f;
    private float _lightMagicBuffDuration = 1f;
    private float _shieldBuffDuration = 2f;

    private bool _isTalentActive = false;
    
    private List<StatusEffect> _effects = new();
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State => States.EmeraldSkin;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    protected override void OnEnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        duration = durationToExit;
        _isTalentActive = damageToExit > 0;
        
        ApplyBuff();
        
        foreach (var skill in characterState.Character.Abilities.Abilities)
        {
            if (skill.Info.School == Schools.Light && _isTalentActive)
            {
                skill.CastEnded += AddTimeByLightMagic;
            }

            switch (skill.Name)
            {
                case "FlashOfLight":
                    skill.CastEnded += AddTimeByFlash;
                    break;
                case "PriestShield":
                    skill.CastEnded += AddTimeByShield;
                    break;
                default:
                    continue;
            }
        }
    }

    public override void OnUpdateState()
    {
    }

    protected override void OnExitState()
    {
        foreach (var skill in characterState.Character.Abilities.Abilities)
        {
            if (skill.Info.School == Schools.Light && _isTalentActive)
            {
                skill.CastEnded -= AddTimeByLightMagic;
            }
            switch (skill.name)
            {
                case "FlashOfLight":
                    skill.CastEnded -= AddTimeByFlash;
                    break;
                case "PriestShield":
                    skill.CastEnded -= AddTimeByShield;
                    break;
                default:
                    continue;
            }
        }

        Debug.Log("Emerald Skin state Exit");
        RemoveBuff();
        characterState.RemoveStateFromList(this);
    }

    /*public override bool Stack(float time)
    {
        _buffDuration += time;
        return true;
    }*/

    private void AddTimeByFlash()
    {
        Debug.Log("Add time by flash - " + _flashBuffDuration);
        duration += _flashBuffDuration;

        characterState.StateIcons?.ActivateIco(State, duration, 1, false);
    }

    private void AddTimeByShield()
    {
        Debug.Log("Add time by shield - " + _shieldBuffDuration);
        duration += _shieldBuffDuration;

        characterState.StateIcons?.ActivateIco(State, duration, 1, false);
    }
    
    private void AddTimeByLightMagic()
    {
        Debug.Log("Add time by light - " + _lightMagicBuffDuration);
        duration += _lightMagicBuffDuration;

        characterState.StateIcons?.ActivateIco(State, duration, 1, false);
    }

    private void ApplyBuff()
    {
        _physDefenseIncrease = _defenseIncrease - characterState.Character.Health.DefPhysDamage;
        _magDefenseIncrease = _defenseIncrease - characterState.Character.Health.DefMagDamage;
        
        characterState.Character.Health.SetPhysicDef(characterState.Character.Health.DefPhysDamage + _physDefenseIncrease);
        characterState.Character.Health.SetMagicDef(characterState.Character.Health.DefMagDamage + _magDefenseIncrease);
    }

    private void RemoveBuff()
    {
        characterState.Character.Health.SetPhysicDef(characterState.Character.Health.DefPhysDamage - _physDefenseIncrease);
        characterState.Character.Health.SetMagicDef(characterState.Character.Health.DefMagDamage - _magDefenseIncrease);
    }
}