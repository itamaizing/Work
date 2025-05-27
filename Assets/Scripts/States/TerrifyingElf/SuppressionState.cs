using System.Collections.Generic;
using UnityEngine;

public class SuppressionState : AbstractCharacterState
{
    private const int MaxStacks = 1;
    private const float CellLength = 0.1f;
    private const float ManaLossPerCellPct = 0.001f;

    private GameObject _suppressionEffectIdle;
    private GameObject _suppressionEffectMove;
    private float _baseDuration;
    private float _duration;
    private int _currentStacks = 1;
    private Vector3 _lastPos; 
    private float _distBuffer;

    private static readonly List<StatusEffect> _effects = new() { StatusEffect.Move };

    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override States State => States.Suppression;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character caster, string skillName)
    {
        _characterState = character;
        _personWhoMadeBuff = caster;

        _baseDuration = durationToExit;
        _duration = _baseDuration;

        _lastPos = character.Character.transform.position;
        _distBuffer = 0f;

        if (_characterState.StateEffects.SuppressionIdle != null && _characterState.StateEffects.SuppressionMove != null)
        {
            _suppressionEffectIdle = _characterState.StateEffects.SuppressionIdle;
            _suppressionEffectMove = _characterState.StateEffects.SuppressionMove;
            _suppressionEffectIdle.SetActive(true);
        }
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;
        if (_duration <= 0f)
        {
            ExitState();
            return;
        }

        TrackMovementAndDrainMana();
    }

    public override void ExitState()
    {
        if (_suppressionEffectIdle != null) _suppressionEffectIdle.SetActive(false);
        if (_suppressionEffectMove != null) _suppressionEffectMove.SetActive(false);
        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        if (_currentStacks < MaxStacks) _currentStacks++;

        _duration = _baseDuration;
        return true;
    }

    private void TrackMovementAndDrainMana()
    {
        Vector3 curPos = _characterState.Character.transform.position;
        float delta = Vector3.Distance(_lastPos, curPos);

        if (delta <= 0f) return;

        _distBuffer += delta;
        _lastPos = curPos;

        int cellsPassed = Mathf.FloorToInt(_distBuffer / CellLength);
        if (cellsPassed <= 0) return;

        _distBuffer -= cellsPassed * CellLength;

        if (_characterState.Character.TryGetResource(ResourceType.Mana) is Mana mana)
        {
            float manaLoss = cellsPassed * mana.MaxValue * ManaLossPerCellPct;
            mana.TryUse(manaLoss);
        }
    }
}
