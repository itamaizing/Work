using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class ImmortalitySkill : Skill
{
    [SerializeField] private float _immortalityTime = 4f;
    
    protected override int AnimTriggerCastDelay { get; }
    protected override int AnimTriggerCast => Animator.StringToHash("Immortality");
    public override void LoadTargetData(TargetInfo targetInfo) { }
    
    public void AnimCastImmortality()
    {
        AnimStartCastCoroutine();
    }

    public void AnimImmortalityEnd()
    {
        AnimCastEnded();
    }   

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        yield return null;
    }

    protected override IEnumerator CastJob()
    {
        CmdAddImmortalityState();
        yield return null;
    }

    protected override void ClearData()
    {
    }

    [Command]
    private void CmdAddImmortalityState()
    {
        _hero.CharacterState.AddState(States.ImmortalityState,_immortalityTime,0,_hero.gameObject,nameof(ImmortalitySkill));
    }
}
