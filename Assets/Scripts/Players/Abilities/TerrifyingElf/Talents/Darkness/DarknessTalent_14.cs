using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarknessTalent_14 : Talent
{
    [SerializeField] private Ghost ghost;

    public override void Enter()
    {
        ghost.CooldownGhostShotActiveTalent(true);
    }

    public override void Exit()
    {
        ghost.CooldownGhostShotActiveTalent(false);
    }
}
