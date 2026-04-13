using UnityEngine;

public class HuntressTalent_1 : Talent
{
    [SerializeField] private Ghost ghost;
    [SerializeField] private VisionComponent visionComponent;
    [SerializeField] private SkillManager skillManager;

    private const float Multiplier = 1.5f;

    private bool visualsApplied = false;
    private bool isActiveHuntreesTalent = false;

    public override void Enter()
    {
        if (isActiveHuntreesTalent) return;

        isActiveHuntreesTalent = true;

        ghost.MovingToGhostWithZeroMana(true);
        visionComponent.VisionRange += 3;

        foreach (Skill skill in skillManager.Abilities)
        {
            if (skill == null) continue;

            skill.Buff.Length.IncreasePercentage(Multiplier);
            skill.Buff.Radius.IncreasePercentage(Multiplier);
        }

        if (!visualsApplied && skillManager.Abilities.Count > 0)
        {
            if (skillManager.Abilities[0] != null &&
                skillManager.Abilities[0].TryGetComponent(out SkillRenderer renderer))
            {
                renderer.MultiplyCastVisuals(Multiplier);
                visualsApplied = true;
            }
        }
    }

    public override void Exit()
    {
        if (!isActiveHuntreesTalent) return;

        isActiveHuntreesTalent = false;

        ghost.MovingToGhostWithZeroMana(false);
        visionComponent.VisionRange -= 3;

        foreach (Skill skill in skillManager.Abilities)
        {
            if (skill == null) continue;

            skill.Buff.Length.ReductionPercentage(Multiplier);
            skill.Buff.Radius.ReductionPercentage(Multiplier);
        }

        if (visualsApplied && skillManager.Abilities.Count > 0)
        {
            if (skillManager.Abilities[0] != null &&
                skillManager.Abilities[0].TryGetComponent(out SkillRenderer renderer))
            {
                renderer.DivideCastVisuals(Multiplier);
                visualsApplied = false;
            }
        }
    }
}