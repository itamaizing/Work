using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarknessTalent_4 : Talent
{
    [SerializeField] private Ghost ghost;

    public override void Enter()
    {
        ghost.SendingGhostTargetTalentActive(true);
    }

    public override void Exit()
    {
        ghost.SendingGhostTargetTalentActive(false);
    }
}
