using UnityEngine;

public class HuntressTalent_1 : Talent
{
    [SerializeField] private Ghost ghost;
    [SerializeField] private VisionComponent visionComponent;
    [SerializeField] private SkillManager skillManager;

    private const float Buff = 1.5f;
    private bool visualsApplied = false;

    public override void Enter()
    {
        ghost.MovingToGhostWithZeroMana(true);
        visionComponent.VisionRange += 3;

        foreach (Skill skill in skillManager.Abilities)
        {
            skill.Buff.Length.AddValue(Buff);
            skill.Buff.Radius.AddValue(Buff);
        }

        if (!visualsApplied && skillManager.Abilities.Count > 0)
        {
            if (skillManager.Abilities[0].TryGetComponent(out SkillRenderer renderer))
            {
                renderer.MultiplyCastVisuals(Buff);
                visualsApplied = true;
            }
        }
    }

    public override void Exit()
    {
        ghost.MovingToGhostWithZeroMana(false);
        visionComponent.VisionRange -= 3;

        foreach (Skill skill in skillManager.Abilities)
        {
            skill.Buff.Length.RemoveValue(Buff);
            skill.Buff.Radius.RemoveValue(Buff);
        }

        if (visualsApplied && skillManager.Abilities.Count > 0)
        {
            if (skillManager.Abilities[0].TryGetComponent(out SkillRenderer renderer))
            {
                renderer.DivideCastVisuals(Buff);
                visualsApplied = false;
            }
        }
    }
}
