using System.Collections.Generic;
using UnityEngine;

public class TrueSight : StateBasic
{
    public override States State => States.TrueSightState;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => new();

    public override bool TryApply(CharacterState character, float durationToExit, float damageToExit, Character personMadeBuff, string skillName)
    {
        MaxDuration = durationToExit;
        return base.TryApply(character, durationToExit, damageToExit, personMadeBuff, skillName);
    }

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personMadeBuff, string skillName)
    {
        CheckInvisibility();
    }

    protected override void OnExit()
    {
        var character = characterState.GetComponent<Character>();
        if (characterState.CheckForState(States.Invisible) || characterState.CheckForState(States.CreeperInvisible)) LostInvisibleEnemy(character);
    }

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
