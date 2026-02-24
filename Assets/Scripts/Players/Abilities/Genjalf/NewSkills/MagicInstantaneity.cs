using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MagicInstantaneity : Skill, IPassiveSkill
{
    [SerializeField] private List<Skill> _instantSkills = new();
    [SerializeField] private float _speedBonusMultiplier = 0.8f;
    [SerializeField] private float _buffDuration = 3f;
    [SerializeField] private float _chainBreakTime = 2f;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    private GameObject _lastDamagedTarget;
    private int _chainCount = 0;
    private Coroutine _chainBreakCoroutine;
    private Coroutine _buffCoroutine;
    private List<Skill> _buffedSkills = new();

    public override void LoadTargetData(TargetInfo targetInfo) { }
    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback) { yield break; }
    protected override IEnumerator CastJob() { yield break; }
    protected override void ClearData() { }

    private void OnActive()
    {
        _instantSkills.Clear();
        _instantSkills = _hero.Abilities.Abilities
            .Where(s => s != this && s.CastDeley == 0 && s.CastStreamDuration == 0 && s.IsSkillActive)
            .ToList();

        foreach (var skill in _instantSkills)
            skill.OnDamageApplied += OnInstantSkillDamageApplied;
    }

    private void OnDiactive()
    {
        foreach (var skill in _instantSkills)
            skill.OnDamageApplied -= OnInstantSkillDamageApplied;

        _instantSkills.Clear();
    }

    private void OnInstantSkillDamageApplied(GameObject target)
    {
        if (target == null) return;

        bool isNewTarget = target != _lastDamagedTarget;
        _lastDamagedTarget = target;

        if (!isNewTarget) return;
        
        ApplySpeedBuff();
    }

    private void ApplySpeedBuff()
    {
        var skillsWithDelay = _hero.Abilities.Abilities
            .Where(s => s != this && s.CastDeley > 0)
            .ToList();

        RemoveBuff();
        _buffedSkills = skillsWithDelay;

        foreach (var skill in _buffedSkills)
            skill.Buff.CastSpeed.IncreasePercentage(_speedBonusMultiplier);

        if (_buffCoroutine != null) StopCoroutine(_buffCoroutine);
        _buffCoroutine = StartCoroutine(BuffDurationCoroutine());
    }

    private IEnumerator BuffDurationCoroutine()
    {
        yield return new WaitForSeconds(_buffDuration);
        RemoveBuff();
    }

    private void RemoveBuff()
    {
        foreach (var skill in _buffedSkills)
        {
            if (skill != null)
                skill.Buff.CastSpeed.ReductionPercentage(_speedBonusMultiplier);
        }
        _buffedSkills.Clear();
        _buffCoroutine = null;
    }
}
