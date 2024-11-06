using System.Collections;
using Mirror;
using UnityEngine;

public class SelfHarm : Skill
{
    public void ActivateTalent(bool isActive)
    { 
        IsSkillActive = isActive;
        StartCoroutine(CastJob());
    }

    protected override int AnimTriggerCastDelay => Animator.StringToHash("CastDelay");
    protected override int AnimTriggerCast =>  Animator.StringToHash("Cast");
    protected override bool IsCanCast => false;
    
    public void AnimCastSelfHarm()
    {
        AnimStartCastCoroutine();
    }

    public void AnimCastSelfHarmEnd()
    {
        AnimCastEnded();
    }

    protected override IEnumerator PrepareJob()
    {
        yield break;
    }

    protected override IEnumerator CastJob()
    {
        yield return new WaitForSeconds(0);

        if (IsSkillActive)
        {
            ApplyEffect();
        }
        else
        {
            RemoveEffect();
        }
    }

    private void ApplyEffect()
    {
        CmdAddBaff(States.SelfHarm, -1f, 0, Hero.gameObject, Name);
    }
    
    private void RemoveEffect()
    {
        CmdRemoveBuff(States.SelfHarm, Hero.gameObject);
    }
    
    [Command]
    private void CmdAddBaff(States darkState, float duration, float damagePerTick, GameObject target, string skillName)
    {
        var characterState = target.GetComponent<CharacterState>();
        characterState.AddState(darkState, duration, damagePerTick, target, skillName);
    }
    
    [Command]
    private void CmdRemoveBuff(States state, GameObject target)
    {
        var characterState = target.GetComponent<CharacterState>();
        characterState.RemoveState(state);
    }

    protected override void ClearData()
    {
    }
}
