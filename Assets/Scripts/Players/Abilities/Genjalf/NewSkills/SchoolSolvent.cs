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
    private bool IsEnemyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Enemy");
    
    private float _clickRadius = 0.5f;

    private readonly HashSet<Schools> _accumulatedSchools = new HashSet<Schools>();
    
    #region Talents
    private bool _isApplyDamageTalent;
    private float _manaPercentDamage = 0.3f;
    private float _lastSkillManaCost;
    #endregion
    
    private bool CheckCanCast()
    {
        return Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position) <= AreaInfo.Radius && Targeting.GetTarget()?.Character != null;
    }
    
    public void IsApplyDamageTalent(bool value)
    {
        if (_isApplyDamageTalent == value) return;
        _isApplyDamageTalent = value;
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

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Targeting.GetTempTarget()?.Character);
        targetDataSavedCallback(targetInfo);
        Targeting.ClearTempTarget();
    }

    protected override IEnumerator CastJob()
    {
        var target = Targeting.GetTarget()?.Character;
        if (target == null) yield break;
        CmdDispelAccumulatedSchools(target.gameObject,IsEnemyTarget(target));

        _accumulatedSchools.Clear();

        yield return null;
    }

    [Command]
    private void CmdDispelAccumulatedSchools(GameObject targetGO,bool isEnemy)
    {
        if (targetGO == null) return;

        var state = targetGO.GetComponent<CharacterState>();
        if (state == null) return;

        var statesCopy = state.CurrentStates;

        foreach (var s in statesCopy)
        {
            if (_accumulatedSchools.Contains(s.Schools))
            {
                state.RemoveState(s.State);
                if (s.BaffDebaff == BaffDebaff.Baff && _isApplyDamageTalent)
                {
                    _lastSkillManaCost = s.Skill.Cost.BaseCost;
                    Damage dmg = new Damage();
                    dmg.Value = _lastSkillManaCost * _manaPercentDamage;
                    ApplyDamage(dmg,targetGO);
                }
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
