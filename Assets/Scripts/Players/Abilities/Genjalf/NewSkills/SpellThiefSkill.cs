using System;
using System.Collections;
using System.Linq;
using Mirror;
using UnityEngine;

public class SpellThiefSkill : Skill
{
    protected override bool IsCanCast { get => CheckCanCast(); }

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => Animator.StringToHash("SpellThief");
    
    private float _clickRadius = 0.5f;
    
    private bool IsEnemyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Enemy");

    private bool CheckCanCast()
    {
        return GetTargetCharacter().CharacterState.CurrentStates.FirstOrDefault(c => c.BaffDebaff == BaffDebaff.Baff) != null && Vector3.Distance(GetTargetCharacter().transform.position, transform.position) <= Radius && GetTargetCharacter() != null;
    }

    public void AnimCastThief()
    {
        AnimStartCastCoroutine();
    }

    public void AnimThiefEnd()
    {
        AnimCastEnded();
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if(targetInfo.GetTargets().Count > 0 && targetInfo.GetTargets()[0] != null)
            SetTarget(targetInfo.GetTargets()[0]);
    }

    protected override IEnumerator CastJob()
    {
        if (GetTargetCharacter() != null)
        {
            var targetGO = GetTargetCharacter().gameObject;
            
            CmdState(targetGO);
        }
        yield return null;
    }

    protected override void ClearData()
    {
        ClearTarget();
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();

        while (GetTempTarget() == null)
        {
            if (GetMouseButton)
            {
                Vector3 clickPoint = GetMousePoint();
                
                FindTarget(_clickRadius, clickPoint, canTargetHimself: true);

                if (GetTempTargetCharacter() is Character character)
                {
                    if (GetTempTargetCharacter() != null && !IsEnemyTarget(character))
                    {
                        ClearTempTarget();
                    }
                    else
                    {
                        if (character.SelectedCircle != null) character.SelectedCircle.IsActive = false;
                        break;
                    }
                }
            }
            yield return null;
        }
        SetTarget(GetTempTarget());
        ClearTempTarget();

        targetInfo.AddTarget(GetTargetCharacter());
        callbackDataSaved(targetInfo);
    }

    [Command]
    private void CmdState(GameObject enemy)
    {
        Character enemyChar = enemy.GetComponent<Character>();
        var baffState = enemyChar.CharacterState.CurrentStates
            .FirstOrDefault(c => c.BaffDebaff == BaffDebaff.Baff);

        if (baffState == null) return;
        
        enemyChar.CharacterState.RemoveState(baffState.State);
        
        _hero.CharacterState.AddState(baffState.State, baffState.RemainingDuration, 0, Hero.gameObject, name);
    }
}
