using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightTalent_3 : Talent
{
    [SerializeField] PriestPassiveSkill priestPassiveSkill;

    public override void Enter()
    {
        priestPassiveSkill.DamageDarkLightStun(true);
    }

    public override void Exit()
    {
        priestPassiveSkill.DamageDarkLightStun(false);
    }
}
