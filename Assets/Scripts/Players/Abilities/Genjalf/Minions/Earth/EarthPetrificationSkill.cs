using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class EarthPetrificationSkill : MoveSkill
{
    [SerializeField] private float _debuffDuration = 4;
    protected override bool IsCanCast { get => CheckCanCast(); }
    protected override int AnimTriggerCastDelay => 0;
    private bool IsEnemyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Enemy");
    private bool IsAllyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");
    protected override int AnimTriggerCast => Animator.StringToHash("Petrification");

    private float _clickRadius = 0.5f;
    
    public void AnimCastPetrification()
    {
            
        AnimStartCastCoroutine();
    }

    public void AnimPetrificationEnd()
    {
        AnimCastEnded();
    }
    
    private void OnEnable()
    {
        Canceled += CancelMove;
    }

    private void OnDisable()
    {
        Canceled -= CancelMove;
    }
    
    private bool CheckCanCast()
    {
        return Vector3.Distance(Targeting.GetTarget().Character.Position, transform.position) <= AreaInfo.Radius;
    }
    
    public override void LoadTargetData(TargetInfo targetInfo)
    {
        Targeting.SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
        
        if (!IsCanCast)
        {
            MoveTo();
        }
    }

    protected override IEnumerator CastJob()
    {
        Character originalTarget = Targeting.GetTarget()?.Character;
        if (originalTarget == null) yield break;
        
        CmdAddState(originalTarget.gameObject,_debuffDuration);
        yield return null;
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        TargetInfo targetInfo = new TargetInfo();
        while (Targeting.GetTempTarget() == null)
        {
            if (GetMouseButton)
            {
                Vector3 clickPoint = Targeting.GetMousePoint();
        
                Targeting.FindTempTarget(clickPoint, _clickRadius, canTargetSelf: false);
                if (Targeting.GetTempTarget()?.Character is Character character)
                {
                    if (Targeting.GetTempTarget()?.Character != null && !IsEnemyTarget(character) && !IsAllyTarget(character))
                    {
                        Targeting.ClearTempTarget();
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
        targetInfo.AddTarget(Targeting.GetTempTarget()?.Character);
        Targeting.ClearTempTarget();
        targetDataSavedCallback(targetInfo);
    }

    [Command]
    private void CmdAddState(GameObject enemy, float time)
    {
        Character enemyChar = enemy.GetComponent<Character>();
        enemyChar.CharacterState.AddState(States.PetrificationDebuff, time, 0, Hero.gameObject, name);
    }
}
