using UnityEngine;

public class DisciplineTalent_14 : Talent
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
