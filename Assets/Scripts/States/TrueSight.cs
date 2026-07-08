using System.Collections.Generic;
using UnityEngine;

public class TrueSight : AbstractCharacterState
{
    public override States State => States.TrueSightState;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => new();

    protected override void OnEnterState(CharacterState character, float durationToExit, float damageToExit, Character personMadeBuff, string skillName)
    {
        characterState = character;
        abilities = character.Character.GetComponent<SkillManager>();
        health = character.Character.GetComponent<Health>();
        personWhoMadeBuff = personMadeBuff;

        duration = durationToExit;
        //MaxStacksCount = 0;

        CheckInvisibility();
    }

    public override void OnUpdateState()
    {

    }

    protected override void OnExitState()
    {
        var character = characterState.GetComponent<Character>();
        if (characterState.CheckForState(States.Invisible) || characterState.CheckForState(States.CreeperInvisible)) LostInvisibleEnemy(character);
        characterState.RemoveStateFromList(this);
    }

    /*public override bool Stack(float time)
    {
        duration = Mathf.Max(duration, time);
        CheckInvisibility();
        return false;
    }*/

    private void CheckInvisibility()
    {
        var character = characterState.GetComponent<Character>();
        if (characterState.CheckForState(States.Invisible) || characterState.CheckForState(States.CreeperInvisible)) DetectionInvisibleEnemy(character);
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

        invisibleEnemy.Appeared();
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

        invisibleEnemy.Disappeared();
        invisibleEnemy.SelectedCircle?.SetAllProjectorsEnabled(false);
    }
}