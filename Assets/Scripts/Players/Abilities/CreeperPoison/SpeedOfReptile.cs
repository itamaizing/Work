using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class SpeedOfReptile : Skill
{
    [SerializeField] private Character _player;
    [SerializeField] private CreeperStrike _creeperStrike;

    [SerializeField]private float _duration = 3f;

    private float _increaseMoveSpeed = 2f;
    private float _increaseAttackSpeed = 2f;
    private float _increaseEvasion = 2f;
    private AttributeModifier _modif;
    
    private readonly AttributeModifier _evadeMeleeModifier = new(1, ModifierType.Multiplier);
    private readonly AttributeModifier _evadeRangeModifier = new(1, ModifierType.Multiplier);
    private readonly AttributeModifier _evadeMagicModifier = new(1, ModifierType.Multiplier);

    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerCastDelay => 0;
    protected override bool IsCanCast => true;

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        yield return null;
    }

    protected override IEnumerator CastJob()
    {
        IncreaseValues();
        yield return null;
    }

    protected override void ClearData()
    {
    }

    private void IncreaseValues()
    {
        _creeperStrike.SetSpeedOfReptile(true);

        _creeperStrike.Buff.AttackSpeed.ReductionPercentage(_increaseAttackSpeed);
        CmdIncreaseValues();

        Invoke("ResetValues", _duration);
    }

    private void ResetValues()
    {
        _creeperStrike.SetSpeedOfReptile(false);

        _creeperStrike.Buff.AttackSpeed.IncreasePercentage(_increaseAttackSpeed);
        CmdResetValues();
    }

    [Command]
    private void CmdIncreaseValues()
    {
        var attrs = _player.AttributeSystem;

        _evadeMeleeModifier.Source = this;
        _evadeMeleeModifier.Value = _increaseEvasion;
        attrs[CharacterAttributeName.EvasionPhysicalMelee].AddModifier(_evadeMeleeModifier);

        _evadeRangeModifier.Source = this;
        _evadeRangeModifier.Value = _increaseEvasion;
        attrs[CharacterAttributeName.EvasionPhysicalRange].AddModifier(_evadeRangeModifier);

        _evadeMagicModifier.Source = this;
        _evadeMagicModifier.Value = _increaseEvasion;
        attrs[CharacterAttributeName.EvasionMagical].AddModifier(_evadeMagicModifier);

        _modif.Value = _increaseMoveSpeed;
        _modif.Type = ModifierType.Multiplier;
        _player.Move.AddModifier(_modif);
    }

    [Command]
    private void CmdResetValues()
    {
        var attrs = _player.AttributeSystem;
        attrs[CharacterAttributeName.EvasionPhysicalMelee].RemoveModifier(_evadeMeleeModifier);
        attrs[CharacterAttributeName.EvasionPhysicalRange].RemoveModifier(_evadeRangeModifier);
        attrs[CharacterAttributeName.EvasionMagical].RemoveModifier(_evadeMagicModifier);

        _player.Move.RemoveModifier(_modif);
    }
}
