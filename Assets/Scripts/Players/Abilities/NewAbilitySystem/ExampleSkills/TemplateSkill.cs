using System;
using System.Collections;
using Unity.VisualScripting;

public class TemplateSkill : Skill
{
    #region Variables

    #endregion

    protected override int AnimTriggerCastDelay => throw new System.NotImplementedException();

    protected override int AnimTriggerCast => throw new System.NotImplementedException();

    #region Methods
    #region Initialization
    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);
    }

    protected override void Awake()
    {
        base.Awake();
    }
    #endregion Initialization

    #region Boost
    protected override void SkillEnableBoostLogic()
    {
        base.SkillEnableBoostLogic();
    }

    protected override void SkillDisableBoostLogic()
    {
        base.SkillDisableBoostLogic();
    }
    #endregion Boost

    protected override bool IsCanCast => base.IsCanCast;

    #region Preparing
    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        return base.PrepareJob(targetDataSavedCallback);
    }

    protected override IEnumerator TargetingBehaviour(Action<TargetInfo> callbackDataSaved)
    {
        return base.TargetingBehaviour(callbackDataSaved);
    }

    protected override bool SetQueueTarget(TargetData target, Action<TargetInfo> callbackDataSaved)
    {
        return base.SetQueueTarget(target, callbackDataSaved);
    }
    #endregion Preparing

    #region Casting
    public override void LoadTargetData(TargetInfo targetInfo)
    {
        base.LoadTargetData(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        throw new System.NotImplementedException();
    }

    protected override void SpendResources()
    {
        base.SpendResources();
    }

    protected override void ClearData()
    {
        base.ClearData();
    }
    #endregion Casting

    #region Custom Draw Indicator
    public override void StartCustomDraw()
    {

    }
    public override void StopCustomDraw()
    {

    }
    public override IEnumerator CustomDrawJob(float time = 0.2f)
    {
        yield return null; //new WaitForSeconds(time);
    }
    #endregion Custom Draw Indicator

    #endregion Methods
}