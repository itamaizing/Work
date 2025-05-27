using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ElvenSkill : AbstractCharacterState
{
    private float _duration;
    private MoveComponent _move;

    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override States State => States.ElvenSkill;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Ability };

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _duration = durationToExit;
        _characterState = character;
        _personWhoMadeBuff = personWhoMadeBuff;
        _move = character.GetComponent<MoveComponent>();

        if (_characterState.TryGetComponent<Character>(out var ability))
        {
            foreach (var skillPhysics in ability.Abilities.Abilities.Where(skillPhysics => skillPhysics.DamageType == DamageType.Physical))
            {
                skillPhysics.CastStarted += OnPhysCastStarted;
                skillPhysics.CastEnded += OnPhysCastFinished;
                skillPhysics.Canceled += OnPhysCastFinished;
            }
        }
    }

    public override void ExitState()
    {
        if (_move) _move.CanMoveState = false;

        if (_characterState.TryGetComponent<Character>(out var ability))
        {
            foreach (var skillPhysics in ability.Abilities.Abilities.Where(skillPhysics => skillPhysics.DamageType == DamageType.Physical))
            {
                skillPhysics.CastStarted -= OnPhysCastStarted;
                skillPhysics.CastEnded -= OnPhysCastFinished;
                skillPhysics.Canceled -= OnPhysCastFinished;
            }
        }

        _characterState.StateIcons.RemoveItemByState(State);
        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        return false;
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;

        if (_duration <= 0) ExitState();
    }

    private void OnPhysCastStarted()  
    {
        if (_move) _move.CanMoveState = true;
    }

    private void OnPhysCastFinished()
    {
        if (_move) _move.CanMoveState = false;
    }
}

