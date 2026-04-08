using System.Collections.Generic;
using UnityEngine;

public class ElvenSkill : RefreshingState
{
    private float _duration;
    private MoveComponent _move;
    private GameObject _elvenSkillEffect;
    private TerrifyingElfAura _aura;
    private SkillManager _skillManager;

    private const float PercentBonusPerStack = 0.1f;
    private const int MaxStacks = 3;

    private int _currentStacks = 0;

    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State => States.ElvenSkill;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    private List<StatusEffect> _effects = new() { StatusEffect.Ability };

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _duration = durationToExit;
        characterState = character;
        base.personWhoMadeBuff = personWhoMadeBuff;

        _move = character.GetComponent<MoveComponent>();
        _skillManager = characterState.Character.Abilities;

        _move.SetCanMoveState(true);

        _currentStacks = 0;

        AddStack();

        if (_skillManager != null)
        {
            foreach (var skill in _skillManager.Abilities)
            {
                if (skill == null) continue;

                if (skill.Info.DamageType == DamageType.Physical || skill.Info.DamageType == DamageType.Both)
                    skill.CastStarted += OnPhysCastStarted;
                else
                    skill.CastStarted += NotPhysCastStarted;

                if (skill is ReconnaissanceFire reconnaissanceFire) reconnaissanceFire.TryStartElvenBoostWindow();
                if (skill is ShotIntoSky shotIntoSky) shotIntoSky.TryStartBoost();
                if (skill is ShotsIntoSky shotsIntoSky) shotsIntoSky.TryStartBoost();
                if (skill is GroundTrap groundTrap) groundTrap.TryStartBoost();
            }
        }

        _aura = character.GetComponent<TerrifyingElfAura>();
        if (_aura != null && _aura.ElvenSkillEffect != null)
        {
            _elvenSkillEffect = _aura.ElvenSkillEffect;
            _elvenSkillEffect.SetActive(true);
        }
    }

    public override bool Stack(float time)
    {
        _duration = time;

        if (_currentStacks >= MaxStacks)
            return false;

        AddStack();
        return true;
    }

    private void AddStack()
    {
        _currentStacks++;

        if (_skillManager == null) return;

        float multiplier = 1 + PercentBonusPerStack;

        foreach (var skill in _skillManager.Abilities)
        {
            if (skill == null) continue;

            skill.Buff.Length.IncreasePercentage(multiplier);
            skill.Buff.Radius.IncreasePercentage(multiplier);
        }
    }

    private void RemoveOneStack()
    {
        if (_skillManager == null) return;

        float multiplier = 1 + PercentBonusPerStack;

        foreach (var skill in _skillManager.Abilities)
        {
            if (skill == null) continue;

            skill.Buff.Length.ReductionPercentage(multiplier);
            skill.Buff.Radius.ReductionPercentage(multiplier);
        }
    }

    public override void ExitState()
    {
        if (_move) _move.SetCanMoveState(false);

        for (int i = 0; i < _currentStacks; i++)
        {
            RemoveOneStack();
        }

        _currentStacks = 0;

        if (_skillManager != null)
        {
            foreach (var skill in _skillManager.Abilities)
            {
                if (skill == null) continue;

                if (skill.Info.DamageType == DamageType.Physical || skill.Info.DamageType == DamageType.Both)
                    skill.CastStarted -= OnPhysCastStarted;
                else
                    skill.CastStarted -= NotPhysCastStarted;
            }
        }

        if (_elvenSkillEffect != null)
            _elvenSkillEffect.SetActive(false);

        characterState.StateIcons.RemoveItemByState(State);
        characterState.RemoveState(this);
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;

        if (_duration <= 0)
            ExitState();
    }

    private void OnPhysCastStarted()
    {
        if (_move) _move.SetCanMoveState(true);
    }

    private void NotPhysCastStarted()
    {
        if (_move) _move.SetCanMoveState(false);
    }
}