using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarknessTalent_8 : Talent
{
    [SerializeField] private Silence silence;

    public override void Enter()
    {
        silence.WeakeningSilenceTalentActive(true);
    }

    public override void Exit()
    {
        silence.WeakeningSilenceTalentActive(true);
    }
}
