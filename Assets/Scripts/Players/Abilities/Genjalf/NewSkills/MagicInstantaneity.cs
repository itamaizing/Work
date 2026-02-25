using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class MagicInstantaneity : Skill, IPassiveSkill, IDamageGivenModifier
{
    [SerializeField] private float _buffDuration = 3f;
    [SerializeField] private float _chainBreakTime = 2f;
    [SerializeField] private float _invisDamageMultiplier = 2f;

    private List<Skill> _instantSkills = new();
    private List<GameObject> _damagedTargets = new();
    private Coroutine _chainBreakCoroutine;
    private bool _nextSkillFromInvisible = false;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    private GameObject _lastDamagedTarget;

    public override void LoadTargetData(TargetInfo targetInfo) { }
    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback) { yield break; }
    protected override IEnumerator CastJob() { yield break; }
    protected override void ClearData() { }

    public void OnActive()
    {
        _instantSkills = _hero.Abilities.Abilities
            .Where(s => s != this && s.CastDeley == 0 && s.IsSkillActive)
            .ToList();

        foreach (var skill in _instantSkills)
            skill.OnDamageApplied += OnInstantSkillDamageApplied;
        
        _hero.Abilities.OnSkillPreparedSuccessfully += OnAnySkillCastStarted;
    }
    
    public void OnDiactive()
    {
        foreach (var skill in _instantSkills)
            skill.OnDamageApplied -= OnInstantSkillDamageApplied;

        _instantSkills.Clear();

        _hero.Abilities.OnSkillPreparedSuccessfully -= OnAnySkillCastStarted;

        ResetChain();
    }
    
    [Command]
    private void OnAnySkillCastStarted(Skill skill)
    {
        if (skill == this) return;

        if (_hero.IsInvisible)
            _nextSkillFromInvisible = true;
    }

    public float ModifyOutgoingDamage(Damage damage)
    {
        if (_nextSkillFromInvisible)
        {
            _nextSkillFromInvisible = false;
            return damage.Value * _invisDamageMultiplier;
        }

        return damage.Value;
    }

    private void OnInstantSkillDamageApplied(GameObject target, Skill skill)
    {
        if (target == null) return;

        bool isNewTarget = !_damagedTargets.Contains(target);

        if (!isNewTarget)
        {
            return;
        }

        _damagedTargets.Add(target);
        RestartChainBreakTimer();

        CmdAddState();
    }

    private void RestartChainBreakTimer()
    {
        if (_chainBreakCoroutine != null)
            StopCoroutine(_chainBreakCoroutine);

        _chainBreakCoroutine = StartCoroutine(ChainBreakCoroutine());
    }

    private IEnumerator ChainBreakCoroutine()
    {
        yield return new WaitForSeconds(_chainBreakTime);
        ResetChain();
    }

    private void ResetChain()
    {
        _damagedTargets.Clear();
        _chainBreakCoroutine = null;
    }

    [Command]
    private void CmdAddState()
    {
        _hero.CharacterState.AddState(States.MagicInstantaneity,_buffDuration,0,_hero.gameObject,nameof(MagicInstantaneity));
    }
}
