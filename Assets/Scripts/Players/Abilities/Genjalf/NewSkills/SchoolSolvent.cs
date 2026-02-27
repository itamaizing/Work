using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SchoolSolvent : Skill
{
    private HashSet<Schools> _schoolsSet;
    protected override int AnimTriggerCastDelay { get; }
    protected override int AnimTriggerCast { get; }
    
    private float _clickRadius = 0.5f;
    
    private bool IsEnemyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Enemy");

    public void AddSchool(Schools school)
    {
        _schoolsSet.Add(school);
    }
    
    public override void LoadTargetData(TargetInfo targetInfo)
    {
        throw new NotImplementedException();
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
                    if (GetTempTargetCharacter() != null)
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

    protected override IEnumerator CastJob()
    {
        if (GetTargetCharacter())
        {
            Character target = GetTargetCharacter();

            foreach (var school in _schoolsSet)
            {
                //target.CharacterState.CurrentStates.Where(c => c.)
            }
        }

        yield return null;
    }

    protected override void ClearData()
    {
        throw new NotImplementedException();
    }
}
