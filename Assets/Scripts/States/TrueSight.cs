using System.Collections.Generic;
using UnityEngine;

public class TrueSight : AbstractCharacterState
{
    public override States State => States.TrueSightState;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => new();

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _characterState = character;
        _abilities = character.Character.GetComponent<SkillManager>();
        _health = character.Character.GetComponent<Health>();
        _personWhoMadeBuff = personWhoMadeBuff;

        duration = durationToExit;
        MaxStacksCount = 0;
        CanStack = false;

        CheckInvisibility();
    }

    public override void UpdateState()
    {
        duration -= Time.deltaTime;

        if (duration <= 0)
        {
            ExitState();
        }

        CheckInvisibility();
    }

    public override void ExitState()
    {
        var character = _characterState.GetComponent<Character>();
        if (_characterState.CheckForState(States.Invisible) || _characterState.CheckForState(States.CreeperInvisible)) LostInvisibleEnemy(character);
        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        duration = Mathf.Max(duration, time);
        return false;
    }

    private void CheckInvisibility()
    {
        var character = _characterState.GetComponent<Character>();
        if (_characterState.CheckForState(States.Invisible) || _characterState.CheckForState(States.CreeperInvisible)) DetectionInvisibleEnemy(character);
    }

    private void DetectionInvisibleEnemy(Character invisibleEnemy)
    {
        SkinnedMeshRenderer renderer = invisibleEnemy.GetComponentInChildren<SkinnedMeshRenderer>();
        if (renderer == null) return;

        foreach (var mat in renderer.materials)
        {
            Color color = mat.color;
            color.a = 0.5f;
            mat.color = color;
        }

        invisibleEnemy.SelectedCircle?.SetAllProjectorsEnabled(true);
    }

    private void LostInvisibleEnemy(Character invisibleEnemy)
    {
        SkinnedMeshRenderer renderer = invisibleEnemy.GetComponentInChildren<SkinnedMeshRenderer>();
        if (renderer == null) return;

        foreach (var mat in renderer.materials)
        {
            Color color = mat.color;
            color.a = 0.0f;
            mat.color = color;
        }

        invisibleEnemy.SelectedCircle?.SetAllProjectorsEnabled(false);
    }
}