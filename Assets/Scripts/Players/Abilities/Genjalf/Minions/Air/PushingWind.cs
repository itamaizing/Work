using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class PushingWind : MoveSkill
{
    [SerializeField] private float _buffDuration = 4;
    protected override bool IsCanCast { get => CheckCanCast(); }
    protected override int AnimTriggerCastDelay => 0;
    private bool IsEnemyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Enemy");
    protected override int AnimTriggerCast => 0;

    private float _clickRadius = 0.5f;

    private bool CheckCanCast()
    {
        return Vector3.Distance(GetTargetCharacter().Position, transform.position) <= Radius;
    }
    
    public override void LoadTargetData(TargetInfo targetInfo)
    {
        SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
        
        if (!IsCanCast)
        {
            MoveTo();
        }
    }

    protected override IEnumerator CastJob()
    {
        Character originalTarget = GetTargetCharacter();
        if (originalTarget == null) yield break;
        
        CmdAddState(originalTarget.gameObject,_buffDuration);
        yield return null;
    }

    protected override void ClearData()
    {
        ClearTarget();
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        TargetInfo targetInfo = new TargetInfo();
        while (GetTempTarget() == null)
        {
            if (GetMouseButton)
            {
                Vector3 clickPoint = GetMousePoint();
        
                FindTarget(_clickRadius, clickPoint, canTargetHimself: false);
                if (GetTempTargetCharacter() is Character character)
                {
                    if (GetTempTargetCharacter() != null && IsEnemyTarget(character))
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
        targetInfo.AddTarget(GetTempTargetCharacter());
        ClearTempTarget();
        targetDataSavedCallback(targetInfo);
    }

    [Command]
    private void CmdAddState(GameObject enemy, float time)
    {
        Character enemyChar = enemy.GetComponent<Character>();
        enemyChar.CharacterState.AddState(States.PushingWindBuff, time, 0, Hero.gameObject, name);
    }
}
