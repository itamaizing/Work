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

    private float _cooldownTimeWithTalent = 4f;

    private bool _isPlayer = false;
    private bool _isCanCrit;
    private bool _isCanCritLightningStrikes;

    private bool _isWaitingForHit = false;

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

    protected override void Awake()
    {
        base.Awake();
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

        CmdApplyImmateriality();
        StartWaitingForHit();

        yield return null;
    }

    /// <summary>
    /// ВАЖНО:
    /// Базовый Skill вызывает CommitUse() после CastJob(),
    /// а CommitUse() вызывает UseCooldownOrCharges().
    /// Поэтому здесь запрещаем старт КД при нажатии ColdBlood.
    /// КД стартует только после усиленного удара.
    /// </summary>
    protected override void UseCooldownOrCharges()
    {
        // Ничего не делаем.
        // Cooldown.Start() будет вызван после OnCreeperStrikeHit().
    }

    protected override void ClearData()
    {
        Debug.Log("ColdBlood / ClearData");

        _mousePosition = Vector3.positiveInfinity;
        _isPlayer = false;

        if (Hero != null && Hero.CharacterState.CheckForState(States.Immateriality))
        {
            Hero.CharacterState.CmdRemoveState(States.Immateriality);
        }
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

        Cooldown.Start();

        ReducingAbilityCooldown();
    }

    public void ReducingAbilityCooldown()
    {
        if (Cooldown.RemainingTime > 0)
        {
            float newCooldownTime = Cooldown.RemainingTime / _reducingCooldownMultiplier;
            Cooldown.SetReduced(newCooldownTime, shouldModify: false);
        }

        RemoveImmaterialityIfActive();
    }

    private void UseAbilityWithTalent()
    {
        if (_isPlayer)
        {
            Cooldown.SetReduced(_cooldownTimeWithTalent, shouldModify: true);

            Debug.Log("ColdBlood / UseAbilityWithTalent / _isPlayer == true");

            Hero.CharacterState.DispelStates(
                StateType.Physical,
                Targeting.GetTarget().Character.NetworkSettings.TeamIndex,
                Hero.NetworkSettings.TeamIndex,
                true
            );
        }
        else
        {
            Debug.Log("ColdBlood / UseAbilityWithTalent / _isPlayer == false");

            _isCanCrit = true;
        }
    }

    private void UseAbilityWithoutTalent()
    {
        Debug.Log("ColdBlood / UseAbilityWithoutTalent");

        _isCanCrit = true;
    }

    private void RemoveImmaterialityIfActive()
    {
        if (Hero != null && Hero.CharacterState.CheckForState(States.Immateriality))
        {
            Hero.CharacterState.CmdRemoveState(States.Immateriality);
        }
    }

    private void OnDisable()
    {
        if (_creeperStrike != null)
            _creeperStrike.OnHit -= OnCreeperStrikeHit;

        _isWaitingForHit = false;
    }

    [Command]
    private void CmdApplyImmateriality()
    {
        Hero.CharacterState.AddState(States.Immateriality, 999, 0, Hero.gameObject, Name);
    }
}