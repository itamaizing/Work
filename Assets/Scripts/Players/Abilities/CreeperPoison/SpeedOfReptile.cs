using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class SpeedOfReptile : Skill
{
    [SerializeField] private Character _player;
    [SerializeField] private CreeperStrike _creeperStrike;

    [SerializeField] private float _duration = 3f;
    [SerializeField] private float _castSpeedMultiplier = 2f;
    [SerializeField] private float _moveSpeedMultiplier = 2f;
    [SerializeField] private float _evasionMultiplier = 2f;
    
    private AttributeModifier _castSpeedModifier;
    private Coroutine _durationJob;

    private AttributeModifier _moveModifier;
    private AttributeModifier _evadeMeleeModifier;
    private AttributeModifier _evadeRangeModifier;
    private AttributeModifier _evadeMagicModifier;

    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerPrepare => 0;
    protected override bool IsCanCast => true;

    public override void LoadTargetData(TargetInfo targetInfo) { }

    protected override IEnumerator TargetingJob(Action<TargetInfo> callbackDataSaved)
    {
        yield return null;
    }

    protected override IEnumerator CastJob()
    {
        ApplyBuff();
        yield return null;
    }

    protected override void ClearData() { }

    private void OnDisable()
    {
        if (_durationJob != null) StopCoroutine(_durationJob);
        _durationJob = null;
        if (_castSpeedModifier != null) RemoveBuff();
    }

    private void ApplyBuff()
    {
        if (_castSpeedModifier != null) RemoveBuff();

        if (_creeperStrike != null) _creeperStrike.SetSpeedOfReptile(true);

        _castSpeedModifier = new AttributeModifier(_castSpeedMultiplier, ModifierType.Multiplier, this);
        _hero.AttributeSystem[CharacterAttributeName.CastSpeedPhysical].AddModifier(_castSpeedModifier);

        CmdApplyServerBuffs();

        if (_durationJob != null) StopCoroutine(_durationJob);
        _durationJob = StartCoroutine(DurationJob());
    }

    private IEnumerator DurationJob()
    {
        yield return new WaitForSeconds(_duration);
        _durationJob = null;
        RemoveBuff();
    }

    private void RemoveBuff()
    {
        if (_castSpeedModifier != null)
        {
            _hero.AttributeSystem[CharacterAttributeName.CastSpeedPhysical].RemoveModifier(_castSpeedModifier);
            _castSpeedModifier = null;
        }

        if (_creeperStrike != null) _creeperStrike.SetSpeedOfReptile(false);

        CmdRemoveServerBuffs();
    }

    [Command]
    private void CmdApplyServerBuffs()
    {
        RemoveServerBuffs();

        var attrs = _player.AttributeSystem;

        _evadeMeleeModifier = new AttributeModifier(_evasionMultiplier, ModifierType.Multiplier, this);
        attrs[CharacterAttributeName.EvasionPhysicalMelee].AddModifier(_evadeMeleeModifier);

        _evadeRangeModifier = new AttributeModifier(_evasionMultiplier, ModifierType.Multiplier, this);
        attrs[CharacterAttributeName.EvasionPhysicalRange].AddModifier(_evadeRangeModifier);

        _evadeMagicModifier = new AttributeModifier(_evasionMultiplier, ModifierType.Multiplier, this);
        attrs[CharacterAttributeName.EvasionMagical].AddModifier(_evadeMagicModifier);

        _moveModifier = new AttributeModifier(_moveSpeedMultiplier, ModifierType.Multiplier, this);
        _player.Move.AddModifier(_moveModifier);
    }

    [Command]
    private void CmdRemoveServerBuffs() => RemoveServerBuffs();

    private void RemoveServerBuffs()
    {
        var attrs = _player.AttributeSystem;

        if (_evadeMeleeModifier != null)
        {
            attrs[CharacterAttributeName.EvasionPhysicalMelee].RemoveModifier(_evadeMeleeModifier);
            _evadeMeleeModifier = null;
        }
        if (_evadeRangeModifier != null)
        {
            attrs[CharacterAttributeName.EvasionPhysicalRange].RemoveModifier(_evadeRangeModifier);
            _evadeRangeModifier = null;
        }
        if (_evadeMagicModifier != null)
        {
            attrs[CharacterAttributeName.EvasionMagical].RemoveModifier(_evadeMagicModifier);
            _evadeMagicModifier = null;
        }
        if (_moveModifier != null)
        {
            _player.Move.RemoveModifier(_moveModifier);
            _moveModifier = null;
        }
    }
}