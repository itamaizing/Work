using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class ColdBlood : Skill
{
    [Header("Ability Properties")]
    [SerializeField] private CreeperStrike _creeperStrike;
    [SerializeField] private float _reducingCooldownMultiplier = 2f;

    private Vector3 _mousePosition = Vector3.positiveInfinity;

    private bool _isPlayer = false;
    private bool _isCanCrit;
    private bool _isCanCritLightningStrikes;

    private bool _isWaitingForHit = false;
    private bool _isColdBloodTalentActive = false;

    public bool IsCanCrit
    {
        get => _isCanCrit;
        set => _isCanCrit = value;
    }

    public bool IsCanCritLightningStrikes
    {
        get => _isCanCritLightningStrikes;
        set => _isCanCritLightningStrikes = value;
    }

    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerCastDelay => 0;

    protected override bool IsCanCast => !_isWaitingForHit;

    public void SetTalentActive(bool value)
    {
        _isColdBloodTalentActive = value;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        Debug.LogError("ColdBlood / LoadTargetData / NonTarget skill should not load target data");
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        yield break;
    }

    protected override IEnumerator CastJob()
    {
        UseAbilityWithoutTalent();

        EnableTargetBoosts();
        StartWaitingForHit();

        yield return null;
    }

    protected override void UseCooldownOrCharges()
    {
    }

    protected override void ClearData()
    {
        _mousePosition = Vector3.positiveInfinity;
        _isPlayer = false;
    }

    private void EnableTargetBoosts()
    {
        _hero.Abilities.GetSkill<CreeperStrike>().EnableSkillBoost();
        _hero.Abilities.GetSkill<LightningStrikes>().EnableSkillBoost();
    }

    private void DisableTargetBoosts()
    {
        _hero.Abilities.GetSkill<CreeperStrike>().DisableSkillBoost();
        _hero.Abilities.GetSkill<LightningStrikes>().DisableSkillBoost();
    }

    private void StartWaitingForHit()
    {
        if (_isWaitingForHit)
            return;

        _isWaitingForHit = true;

        if (_creeperStrike != null)
            _creeperStrike.OnHit += OnCreeperStrikeHit;
        else
            Debug.LogError("ColdBlood / StartWaitingForHit / _creeperStrike is null");
    }

    private void OnCreeperStrikeHit()
    {
        if (_creeperStrike != null)
            _creeperStrike.OnHit -= OnCreeperStrikeHit;

        _isWaitingForHit = false;

        DisableTargetBoosts();
        
        Cooldown.Start();

        if (_isColdBloodTalentActive)
        {
            ReducingAbilityCooldown();
        }
    }

    public void ReducingAbilityCooldown()
    {
        if (Cooldown.RemainingTime > 0)
        {
            Cooldown.StartCustom(Cooldown.CooldownTime / 2);
        }
    }

    private void UseAbilityWithoutTalent()
    {
        _isCanCrit = true;
    }

    private void OnDisable()
    {
        if (_creeperStrike != null)
            _creeperStrike.OnHit -= OnCreeperStrikeHit;

        DisableTargetBoosts();
        _isWaitingForHit = false;
    }
}