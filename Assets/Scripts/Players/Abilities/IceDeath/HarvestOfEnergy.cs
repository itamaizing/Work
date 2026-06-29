using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HarvestOfEnergy : Skill, IComboSeriesParticipatingSkill
{
    [SerializeField] private float rune = 1;
    [SerializeField] private HarvestOfRunes harvestOfRunes;
    protected override int AnimTriggerCastDelay => Animator.StringToHash("SpellCastDelayAnimTrigger");
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => true;
    private bool _isSeriesComplete;
    
    #region HarvestTalent
    
    private bool _harvestTalentEnabled;

    public void SetHarvestTalent(bool enabled)
    {
        if(_harvestTalentEnabled == enabled) return;
        _harvestTalentEnabled = enabled;
    }

    #endregion

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo == null) return;
        if (targetInfo.GetTargets().Contains(Hero)) return;
        targetInfo.AddTarget(Hero);
    }

    protected override void ClearData() { }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Hero);
        callbackDataSaved(targetInfo);

        yield break;
    }

    protected override IEnumerator CastJob()
    {
        if (Hero == null || Hero.CharacterState == null) yield break;
        AddRune();
    }
    
    protected override void CommitUse()
    {
        if (_isSeriesComplete)
        {
            _isSeriesComplete = false;
            SpendResources();
            return;
        }
        base.CommitUse();
    }

    private void AddRune()
    {
        if (Hero.TryGetResource(ResourceType.Rune) is RuneComponent runeAdd) runeAdd.CmdAdd(rune);
        if (harvestOfRunes != null)
            harvestOfRunes.Cooldown.SetIncreased(harvestOfRunes.Cooldown.CooldownTime, shouldModify: false);

        if (_harvestTalentEnabled)
            Hero.Abilities.GetSkill<NinjaResources>()?.CmdSetNextEnergyDamageMultiplier(1.5f);

        OnSeriesDamaged?.Invoke(null, this);
    }

    #region Series
    public bool IsTicking => false;
    public bool IgnoresEnergyCostCheck => true;
    public float EnergyCostOnHit => 0f;
    public float RuneCostOnHit => 0f;

    public event IComboSeriesParticipatingSkill.OnBeforeApplyDamageDelegate OnBeforeApplySeriesDamage;
    public event Action<GameObject, Skill> OnSeriesDamaged;

    public void OnSeriesHit(int hitCountInCurrentSeries, Character target) { }
    public void OnSeriesCompleted(Character target, int totalHits, float totalEnergySpent)
    {
        _isSeriesComplete = true;
    }
    public void OnSeriesBroken(Character target)
    {
        _isSeriesComplete = false;
    }
    public void OnSeriesPotentialFinal(Skill skill, bool isPotentialFinal) { }
    #endregion
}
