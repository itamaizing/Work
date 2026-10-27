using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface IColdBloodEmpowerable { }

public class ColdBlood : Skill
{
    public const float PoisonCritMultiplier = 2.5f;

    [Header("Ability Properties")]
    [SerializeField] private CreeperStrike _creeperStrike;
    [SerializeField] private float _reducingCooldownMultiplier = 2f;

    private Vector3 _mousePosition = Vector3.positiveInfinity;

    private bool _isPlayer = false;
    private bool _isCanCrit;
    private bool _isCanCritLightningStrikes;

    private bool _isWaitingForHit = false;
    private bool _isColdBloodTalentActive = false;
    private bool _isPoisonEmpowerActive = false;

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
    protected override int AnimTriggerPrepare => 0;

    protected override bool IsCanCast => !_isWaitingForHit;

    public void SetTalentActive(bool value)
    {
        _isColdBloodTalentActive = value;
    }

    public void SetPoisonEmpowerActive(bool value)
    {
        _isPoisonEmpowerActive = value;
    }

    public bool TryConsumeForPoison(Character target)
    {
        if (target == null) return false;
        return TryConsume(target.CharacterState.CheckForState(States.Blind));
    }
    
    public bool TryConsumeForPoison(IEnumerable<Character> targets)
    {
        if (targets == null) return false;

        bool any = false;
        bool anyBlind = false;
        foreach (var target in targets)
        {
            if (target == null) continue;

            any = true;
            if (target.CharacterState.CheckForState(States.Blind))
            {
                anyBlind = true;
                break;
            }
        }

        return any && TryConsume(anyBlind);
    }

    private bool TryConsume(bool blindTarget)
    {
        if (!_isPoisonEmpowerActive) return false;

        if (_isWaitingForHit && _isCanCrit)
        {
            _isCanCrit = false;
            _isCanCritLightningStrikes = false;
            FinishWaitingForHit(blindTarget);
            return true;
        }

        if (_isCanCritLightningStrikes)
        {
            _isCanCritLightningStrikes = false;
            return true;
        }

        return false;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
    }

    protected override IEnumerator TargetingJob(Action<TargetInfo> callbackDataSaved)
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

    private readonly List<Skill> _boostedPoisonSkills = new();

    private void EnableTargetBoosts()
    {
        _hero.Abilities.GetSkill<CreeperStrike>().EnableSkillBoost();
        _hero.Abilities.GetSkill<LightningStrikes>().EnableSkillBoost();

        _boostedPoisonSkills.Clear();
        if (!_isPoisonEmpowerActive) return;

        foreach (var skill in _hero.Abilities.Skills.OfType<IColdBloodEmpowerable>().Cast<Skill>())
        {
            skill.EnableSkillBoost();
            _boostedPoisonSkills.Add(skill);
        }
    }

    private void DisableTargetBoosts()
    {
        _hero.Abilities.GetSkill<CreeperStrike>().DisableSkillBoost();
        _hero.Abilities.GetSkill<LightningStrikes>().DisableSkillBoost();

        foreach (var skill in _boostedPoisonSkills)
            if (skill != null) skill.DisableSkillBoost();
        _boostedPoisonSkills.Clear();
    }

    private void StartWaitingForHit()
    {
        if (_isWaitingForHit)
            return;

        _isWaitingForHit = true;

        if (_creeperStrike != null)
            _creeperStrike.OnHit += OnCreeperStrikeHit;
    }

    private void OnCreeperStrikeHit()
    {
        var target = _creeperStrike != null ? _creeperStrike.LastHitTarget : null;
        FinishWaitingForHit(target != null && target.CharacterState.CheckForState(States.Blind));
    }

    private void FinishWaitingForHit(bool hitBlindTarget)
    {
        if (_creeperStrike != null)
            _creeperStrike.OnHit -= OnCreeperStrikeHit;

        _isWaitingForHit = false;

        DisableTargetBoosts();

        bool reduceCooldown = _isColdBloodTalentActive && hitBlindTarget;
        
        if (reduceCooldown) Cooldown.StartCustom(Cooldown.CooldownTime / _reducingCooldownMultiplier);
        else Cooldown.Start();
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