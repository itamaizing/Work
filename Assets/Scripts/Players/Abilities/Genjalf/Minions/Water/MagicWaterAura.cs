using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class MagicWaterAura : AuraStateHandler
{
    [SerializeField] private float _buffDuration = -1f;

    protected override void OnTargetEnter(Character target)
    {
        CmdApplyStateToTarget(target.gameObject, States.MagicWater, _buffDuration, Schools.Water, _owner.gameObject,
            nameof(MagicWater),0);
    }

    protected override void OnTargetExit(Character target)
    {
        CmdRemoveStateFromTarget(target.gameObject, States.MagicWater);
    }

    protected override void OnAuraDisabled()
    {
        RemoveEffectsFromAllTargets();
    }
}

public class MagicWater : AbstractCharacterState
{
    private Character _character;
    private Resource _mana;
    private List<StatusEffect> _effects = new List<StatusEffect>();
    private float _manaRegenProcent = 0.003f;
    private float _manaMaxProcent = 0.1f;

    private float _originalRegenValue = 0;
    private float _currentDelta = 0;

    public override States State => States.MagicWater;
    public override StateType Type { get; }
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit,
        Character personWhoMadeBuff, string skillName)
    {
        duration = durationToExit;
        _character = character.Character;
        if (_character.Resources.Count > 0)
        {
            _character.Resources.TryGetValue(ResourceType.Mana, out _mana);
            if (_mana != null)
            {
                _originalRegenValue = _mana.RegenerationValue;
                _mana.RegenerationValue += _mana.MaxValue * _manaRegenProcent;
                _currentDelta = _mana.MaxValue * _manaMaxProcent;
                _mana.AddMax(_currentDelta, true);
            }
        }
    }

    private void RestoreMana()
    {
        if (_mana != null)
        {
            _mana.RegenerationValue = _originalRegenValue;
            _mana.AddMax(-_currentDelta, true);
        }
    }

    public override void UpdateState()
    {
    }

    public override void ExitState()
    {
        base.ExitState();
        RestoreMana();

        _mana = null;
        _character = null;
    }
}
