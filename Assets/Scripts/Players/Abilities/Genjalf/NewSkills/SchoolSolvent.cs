using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class SchoolSolvent : Skill
{
    [SerializeField] private CounterSpell _counterSpell;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => CheckCanCast();
    
    private float _clickRadius = 0.5f;

    private readonly HashSet<Schools> _accumulatedSchools = new HashSet<Schools>();
    
    private bool CheckCanCast()
    {
        return Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position) <= AreaInfo.Radius && Targeting.GetTarget()?.Character != null;
    }

    private void OnEnable()
    {
        if (_counterSpell != null)
            _counterSpell.OnSpellDispelled += AddSchool;
    }

    private void OnDisable()
    {
        if (_counterSpell != null)
            _counterSpell.OnSpellDispelled -= AddSchool;
    }

    public void AddSchool(Schools school)
    {
        if(!isActiveAndEnabled) return;
        if (school == Schools.None) return;
        _accumulatedSchools.Add(school);
        CmdAddSchool(school);
    }

    [Command]
    private void CmdAddSchool(Schools school)
    {
        _accumulatedSchools.Add(school);
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0)
            Targeting.SetTarget(targetInfo.GetTargets()[0]);
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        while (Targeting.GetTempTarget()?.Character == null)
        {
            if (GetMouseButton)
            {
                Vector3 clickPoint = Targeting.GetMousePoint();

                Targeting.FindTempTarget(clickPoint, _clickRadius, canTargetSelf: true);

            }
            yield return null;
        }
        Targeting.SetTarget(Targeting.GetTempTarget()?.Character);
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Targeting.GetTarget()?.Character);
        targetDataSavedCallback(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        var target = Targeting.GetTarget()?.Character;
        if (target == null) yield break;
        CmdDispelAccumulatedSchools(target.gameObject);

        _accumulatedSchools.Clear();

        yield return null;
    }

    [Command]
    private void CmdDispelAccumulatedSchools(GameObject targetGO)
    {
        if (targetGO == null) return;

        var state = targetGO.GetComponent<CharacterState>();
        if (state == null) return;

        var statesCopy = state.CurrentStates;

        foreach (var s in statesCopy)
        {
            Debug.LogError(s.Schools);
            if (_accumulatedSchools.Contains(s.Schools))
            {
                state.RemoveState(s.State);
            }
        }
        statesCopy.Clear();
        _accumulatedSchools.Clear();
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
    }
}
