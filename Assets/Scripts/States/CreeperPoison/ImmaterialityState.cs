using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImmaterialityState : AbstractCharacterState
{
    private float _duration;
    private float _baseDuration;
    private Character _player;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Immateriality };
    public override float TEST_ChangeableValue { get; set; }
    public override States State => States.Immateriality;
    public override StateType Type => StateType.Immaterial;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _characterState = character;
        _player = _characterState.Character;
        Debug.Log("enterState Immateriality");
        _duration = durationToExit;
        _baseDuration = _duration;

        DisabledCollider();
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;
        if (_duration <= 0)
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        //_player.Collider.enabled = true; 
        _player.Rb.isKinematic = false;
        _duration = 0;
        _baseDuration = 0;
        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        return false;
    }

    private void DisabledCollider()
    {
        if (_player != null)
        {
            Debug.Log("DisabledCollider if player != null / _player.collider == " + _player.Collider);
            _player.Rb.isKinematic = true;
            //_player.Collider.enabled = false;
        }
    }
}