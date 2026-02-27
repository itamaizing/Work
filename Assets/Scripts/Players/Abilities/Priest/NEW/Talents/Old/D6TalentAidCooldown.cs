using UnityEngine;

public class D6TalentAidCooldown : Talent
{
    [SerializeField] private SoulAid _soulAid;

    public override void Enter()
    {
        _soulAid.EnableCooldownReduce(true);
    }

    public override void Exit()
    {
        _soulAid.EnableCooldownReduce(false);
    }
}
