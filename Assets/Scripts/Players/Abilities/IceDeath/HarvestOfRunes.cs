using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class HarvestOfRunes : Skill,IComboSeriesParticipatingSkill
{
    [SerializeField] private float enegry = 70;
    [SerializeField] private HarvestOfEnergy harvestOfEnergy;
    
    private bool _isSeriesComplete;

    protected override int AnimTriggerCastDelay => Animator.StringToHash("SpellCastDelayAnimTrigger");
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => true;

    #region HarvestTalent

    private bool _harvestTalentEnabled;
    private int  _corpseEnemyIndex = 0;
    
    public void SetHarvestTalent(bool value)
    {
        if(value == _harvestTalentEnabled) return;
        _harvestTalentEnabled = value;
    }
    private void SpawnCorpse(int enemyIndex)
    {
        Vector3 pos = _hero.transform.position
                      + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, UnityEngine.Random.Range(-1f, 1f));
        pos.y = _hero.transform.position.y;
        _hero.SpawnComponent.CmdSpawnAliesPoint(pos, Quaternion.identity, enemyIndex);
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
        AddEnergy();
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


    private void AddEnergy()
    {
        if (Hero.TryGetResource(ResourceType.Energy) is Energy energy) energy.CmdAdd(enegry);
        if (harvestOfEnergy != null)
            harvestOfEnergy.Cooldown.SetIncreased(harvestOfEnergy.Cooldown.CooldownTime, shouldModify: false);
        if (_harvestTalentEnabled)
            SpawnCorpse(_corpseEnemyIndex);

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
