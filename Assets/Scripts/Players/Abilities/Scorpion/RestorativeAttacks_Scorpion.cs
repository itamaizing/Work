using System;
using System.Collections;
using UnityEngine;

public class RestorativeAttacks_Scorpion : Skill
{
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => true;

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);

        foreach (var skill in Hero.Abilities.Abilities)
        {
            if (skill is IComboParticipatingSkill combo)
            {
                combo.OnDamaged += OnAttackApplied;
            }
        }
    }

    private void OnDisable()
    {
        if (Hero == null) return;
        foreach (var skill in Hero.Abilities.Abilities)
        {
            if (skill is IComboParticipatingSkill combo)
                combo.OnDamaged -= OnAttackApplied;
        }
    }

    private void OnAttackApplied(GameObject target, Skill sourceSkill)
    {
        if (Hero.CharacterState.CheckForState(States.RestorativeAttacks))
        {
            var state = Hero.CharacterState.GetState(States.RestorativeAttacks) as RestorativeAttacksState;
            state?.OnAttackHit(sourceSkill);
        }
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        callbackDataSaved(new TargetInfo());
        yield return null;
    }

    protected override IEnumerator CastJob()
    {
        CommitUse();
        
        if(isClient)
            Hero.CharacterState.CmdAddState(States.RestorativeAttacks, 3f, 0f, Schools.None, Hero.gameObject, nameof(RestorativeAttacks_Scorpion));

        yield return null;
    }

    protected override void ClearData() { }

    public override void LoadTargetData(TargetInfo targetInfo) { }
}