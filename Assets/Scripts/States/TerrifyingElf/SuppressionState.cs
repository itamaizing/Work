using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuppressionState : AbstractCharacterState
{
    private float _baseDuration;
    private float _duration;
    private int _currentStacks = 1;
    private const int _maxStacks = 1;
    private Vector3 _lastPosition;
    private const float ManaLossPerMeter = 0.01f;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Move };
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override States State => States.Suppression;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        Debug.Log("Entering Suppression State");
        _characterState = character;
        _personWhoMadeBuff = personWhoMadeBuff;
        _baseDuration = durationToExit;
        _duration = _baseDuration;

        _lastPosition = character.Character.transform.position;
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;
        if (_duration <= 0)
        {
            ExitState();
            return;
        }

        TrackMovementAndDrainMana();
    }

    public override void ExitState()
    {
        Debug.Log("Exiting Suppression State");
        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        if (_currentStacks < _maxStacks)
        {
            _currentStacks++;
            _duration = _baseDuration;
            Debug.Log($"Stacking Suppression. Current stacks: {_currentStacks}, New duration: {_duration}s");
            return true;
        }
        else
        {
            _duration = _baseDuration;
            Debug.Log($"Max stacks reached. Refreshing Suppression duration: {_duration}s");
            return false;
        }
    }

    private void TrackMovementAndDrainMana()
    {
        Vector3 currentPosition = _characterState.Character.transform.position;
        float distanceMoved = Vector3.Distance(_lastPosition, currentPosition);

        if (distanceMoved > 0)
        {
            Resource manaResource = _characterState.Character.TryGetResource(ResourceType.Mana);
            if (manaResource != null)
            {
                float manaLoss = manaResource.CurrentValue * ManaLossPerMeter * distanceMoved;
                manaResource.TryUse(manaLoss);
                Debug.Log($"Mana drained: {manaLoss}. Current mana: {manaResource.CurrentValue}");
            }
            _lastPosition = currentPosition;
        }
    }
}
