using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RestorativeAttacksState : AbstractCharacterState
{
    public override States State => States.RestorativeAttacks;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => new List<StatusEffect>();

    private const float EnergyRestorePercent = 0.2f;
    private const int SameSkillsToBreak = 3;

    private readonly List<Skill> _lastHits = new();
    private Resource _energy;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit,
        Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        base.personWhoMadeBuff = personWhoMadeBuff;
        _lastHits.Clear();

        character.Character.TryGetResource(ResourceType.Energy, out _energy);
    }

    public override void UpdateState() { }

    public void OnAttackHit(Skill sourceSkill)
    {
        if (_energy != null)
        {
            float restoreAmount = _energy.MaxValue * EnergyRestorePercent;
            _energy.Add(restoreAmount);
        }

        _lastHits.Add(sourceSkill);

        if (_lastHits.Count >= SameSkillsToBreak)
        {
            var last = _lastHits.Skip(_lastHits.Count - SameSkillsToBreak).ToList();
            if (last.All(s => s == last[0]))
            {
                ExitState();
                return;
            }
        }
    }

    public override void ExitState()
    {
        characterState?.RemoveState(this);
        _lastHits.Clear();
        _energy = null;
    }
}