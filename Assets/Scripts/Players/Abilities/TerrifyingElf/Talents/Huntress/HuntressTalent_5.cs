using UnityEngine;

public class HuntressTalent_5 : Talent
{
    [SerializeField] private ReconnaissanceFire reconnaissanceFire;
    [SerializeField] private TerrifyingElfAura terrifyingElfAura;

    public override void Enter()
    {
        terrifyingElfAura.CalmnessOnElvenSkillTalent(true);
    }

    public override void Exit()
    {
        terrifyingElfAura.CalmnessOnElvenSkillTalent(false);
    }
}
