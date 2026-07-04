using System.Collections.Generic;
using UnityEngine;

public class ImmortalityState : AbstractCharacterState
{
    private float _duration;
    private Character _player;
    private float _savedBlockChance;
    private float _savedEvadeMelee;
    private float _savedEvadeRange;
    private float _savedResistMag;

    private List<StatusEffect> _effects = new();
    public override States State => States.ImmortalityState;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        _player = characterState.Character;
        _duration = durationToExit;

        _savedBlockChance = _player.Health.BlockChance;
        _savedEvadeMelee  = _player.Health.EvadeMeleeDamage;
        _savedEvadeRange  = _player.Health.EvadeRangeDamage;
        _savedResistMag   = _player.Health.ResistMagDamage;

        _player.Health.BlockChance       = 100f;
        _player.Health.EvadeMeleeDamage  = 100f;
        _player.Health.EvadeRangeDamage  = 100f;
        _player.Health.ResistMagDamage   = 100f;
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;
        if (_duration <= 0)
            ExitState();
    }

    public override void ExitState()
    {
        _duration = 0;

        if (_player != null && _player.Health != null)
        {
            _player.Health.BlockChance       = _savedBlockChance;
            _player.Health.EvadeMeleeDamage  = _savedEvadeMelee;
            _player.Health.EvadeRangeDamage  = _savedEvadeRange;
            _player.Health.ResistMagDamage   = _savedResistMag;
        }

        characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        return false;
    }
}

