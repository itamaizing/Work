using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicVarietySkill : Skill, IPassiveSkill
{
    [SerializeField, Min(2)] private int _chainLength = 3;
    [SerializeField, Range(0f, 1f)] private float _manaDiscount = 0.7f;
    [SerializeField, Min(0f)] private float _maxPauseBetweenSpells = 10f;

    private readonly List<Schools> _chain = new();
    private readonly List<Skill> _discounted = new();
    private readonly Dictionary<Skill, Action> _handlers = new();
    private Coroutine _pauseRoutine;
    private bool _enabled;
    private bool _armed;

    public bool IsEnabled => _enabled;

    #region Skill
    protected override int AnimTriggerPrepare => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => false;
    public override void LoadTargetData(TargetInfo targetInfo) { }
    protected override IEnumerator TargetingJob(Action<TargetInfo> cb) { yield break; }
    protected override IEnumerator CastJob() { yield break; }
    protected override void ClearData() { }
    #endregion

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);

        _isSubjectToGlobalCooldownTime = false;

        foreach (var skill in hero.Abilities.Abilities) Watch(skill);
        hero.Reset += ResetChain;

        SetEnabled(false);
    }

    private void OnDestroy()
    {
        foreach (var kv in _handlers)
            if (kv.Key != null) kv.Key.CastSuccess -= kv.Value;
        _handlers.Clear();

        Disarm();
        if (_hero != null) _hero.Reset -= ResetChain;
    }

    public void SetEnabled(bool value)
    {
        if (_enabled == value) return;
        _enabled = value;
        if (!value) ResetChain();
    }

    private void Watch(Skill skill)
    {
        if (skill == null || skill == this || skill is IPassiveSkill || _handlers.ContainsKey(skill)) return;

        Action handler = () => OnSpellCast(skill);
        _handlers[skill] = handler;
        skill.CastSuccess += handler;
    }

    private static bool IsCountedSpell(Skill skill, out Schools school)
    {
        school = skill.Info.School;
        if (skill.Info.AbilityForm != AbilityForm.Magic) return false;
        if (school == Schools.None || school == Schools.Physical) return false;

        foreach (var cost in skill.Cost.Values)
            if (cost.costType == SkillCostType.Mandatory && cost.type == ResourceType.Mana) return true;
        return false;
    }

    private void OnSpellCast(Skill skill)
    {
        if (!_enabled || !_hero.isOwned) return;
        if (!IsCountedSpell(skill, out Schools school)) return;

        if (_armed)
        {
            bool wasDiscounted = !_chain.Contains(school);
            Disarm();
            _chain.Clear();

            if (wasDiscounted)
            {
                StopPauseTimer();
                return;
            }
        }

        if (_chain.Contains(school)) _chain.Clear();
        _chain.Add(school);

        if (_chain.Count == _chainLength - 1) Arm();
        RestartPauseTimer();
    }

    private void Arm()
    {
        _armed = true;

        foreach (var skill in _handlers.Keys)
        {
            if (skill == null || !IsCountedSpell(skill, out Schools school) || _chain.Contains(school)) continue;

            skill.Attributes[SkillAttributeName.ResourceCost]
                 .AddModifier(new AttributeModifier(1f - _manaDiscount, ModifierType.Multiplier, this));
            _discounted.Add(skill);
        }
    }

    private void Disarm()
    {
        foreach (var skill in _discounted)
            if (skill != null)
                skill.Attributes[SkillAttributeName.ResourceCost].RemoveBySource(this, true);

        _discounted.Clear();
        _armed = false;
    }

    private void ResetChain()
    {
        Disarm();
        _chain.Clear();
        StopPauseTimer();
    }

    #region Пауза между заклинаниями
    private void RestartPauseTimer()
    {
        StopPauseTimer();
        if (_maxPauseBetweenSpells > 0f) _pauseRoutine = StartCoroutine(PauseJob());
    }

    private void StopPauseTimer()
    {
        if (_pauseRoutine != null) StopCoroutine(_pauseRoutine);
        _pauseRoutine = null;
    }

    private IEnumerator PauseJob()
    {
        yield return new WaitForSeconds(_maxPauseBetweenSpells);
        _pauseRoutine = null;
        ResetChain();
    }
    #endregion
}