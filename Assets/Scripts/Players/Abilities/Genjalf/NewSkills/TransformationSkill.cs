using System;
using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TransformationSkill : Skill
{
    [SerializeField] private Mesh _meshTranformation;
    [SerializeField] private float _debuffDuration = 6;

    protected override bool IsCanCast { get => CheckCanCast(); }

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;
    
    private float _clickRadius = 0.5f;
    
    private bool IsEnemyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Enemy");

    private bool CheckCanCast()
    {
        return Vector3.Distance(GetTargetCharacter().transform.position, transform.position) <= Radius && GetTargetCharacter() != null;
    }
    
    public override void LoadTargetData(TargetInfo targetInfo)
    {
    }

    protected override IEnumerator CastJob()
    {
        if (GetTargetCharacter() != null)
        {
            var targetGO = GetTargetCharacter();
            
            CmdAddState(targetGO.gameObject,_debuffDuration);
            
            CmdTransformation(targetGO.gameObject);
        }
        yield return null;
    }

    [ClientRpc]
    private void MakeEnemyTransformation(GameObject target)
    {
        target.GetComponent<Character>().TransformationComponent.MakeTransformation(_meshTranformation);
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
    private void CmdAddState(GameObject enemy, float time)
    {
        Character enemyChar = enemy.GetComponent<Character>();
        enemyChar.CharacterState.AddState(States.TransformationDebuff, time, 0, Hero.gameObject, name);
    }
    
    [Command]
    private void CmdTransformation(GameObject target)
    {
        MakeEnemyTransformation(target);
    }
}
