using System;
using System.Collections;

public class ReptilianStasis : Skill
{
    private const float Duration = 1;

    protected override int AnimTriggerPrepare => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => true;

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo == null) return;
        if (targetInfo.GetTargets().Contains(Hero)) return;

        targetInfo.AddTarget(Hero);
    }

    protected override void ClearData() { }
    protected override IEnumerator CastJob()
    {
        if (Hero == null || Hero.CharacterState == null)
            yield break;

        Hero.CharacterState.CmdAddState(States.ReptilianStasis, Duration, 0f, Hero.gameObject, name);

        yield return null; 

        foreach (var skill in Hero.Abilities.Skills)
        {
            if (skill == null || skill == this) continue;
            if (skill.IsTargeting || skill.IsExecuting)
                skill.TryCancel(true);
        }
    }
}

