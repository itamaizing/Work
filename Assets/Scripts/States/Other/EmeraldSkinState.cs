using System.Collections.Generic;
using UnityEngine;

public class EmeraldSkinState : StateBasic
{
    private float _buffDuration = 2f;
    private float _defenseIncrease = 0.9f;

    private float _flashBuffDuration = 1f;
    private float _lightMagicBuffDuration = 1f;
    private float _shieldBuffDuration = 2f;

    private bool _isTalentActive = false;

    private List<StatusEffect> _effects = new();
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State => States.EmeraldSkin;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        _buffDuration = durationToExit;
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

    public override void UpdateState()
    {
        _buffDuration -= Time.deltaTime;

        if (_buffDuration <= 0)
        {
            ExitState();
        }
    }

    public override void ExitState()
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
        characterState.RemoveState(this);
    }

    private void AddTimeByFlash()
    {
        Debug.Log("Add time by flash - " + _flashBuffDuration);
        _buffDuration += _flashBuffDuration;
    }

    private void AddTimeByShield()
    {
        Debug.Log("Add time by shield - " + _shieldBuffDuration);
        _buffDuration += _shieldBuffDuration;
    }

    private void AddTimeByLightMagic()
    {
        Debug.Log("Add time by light - " + _lightMagicBuffDuration);
        _buffDuration += _lightMagicBuffDuration;
    }

    private void ApplyBuff()
    {
        var attrs = characterState.Character.AttributeSystem;
        var physRes = attrs[CharacterAttributeName.ResistancePhysical];
        var magRes = attrs[CharacterAttributeName.ResistanceMagical];

        physRes.AddModifier(new AttributeModifier(_defenseIncrease - physRes.GetValue(), ModifierType.Flat, this));
        magRes.AddModifier(new AttributeModifier(_defenseIncrease - magRes.GetValue(), ModifierType.Flat, this));
    }

    private void RemoveBuff()
    {
        characterState.Character.AttributeSystem[CharacterAttributeName.ResistancePhysical].RemoveBySource(this);
        characterState.Character.AttributeSystem[CharacterAttributeName.ResistanceMagical].RemoveBySource(this);
    }
}