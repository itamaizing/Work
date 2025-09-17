using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarkTalent_1 : Talent
{
    [SerializeField] PriestPassiveSkill priestPassiveSkill;

    public override void Enter()
    {
        priestPassiveSkill.DamageDarkHealLightAddHealth(true);
    }

    public override void Exit()
    {
        priestPassiveSkill.DamageDarkHealLightAddHealth(false);
    }
}
