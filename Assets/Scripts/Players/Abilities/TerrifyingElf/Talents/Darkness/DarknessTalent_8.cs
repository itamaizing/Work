using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarknessTalent_8 : Talent
{
    //#Перенесено - не проверено
    public override void Enter()
    {
        character.Abilities.GetSkill<Ghost>().CooldownGhostShotActiveTalent(true);
        character.Abilities.GetSkill<PullingHealth>().SetPullingHealthGhostTalentActive(true);
        //character.Abilities.GetSkill<Silence>().SilenceEffectsOnMinionMagic(true);
        //character.Abilities.GetSkill<Silence>().GhostDeathSilence(true);
        //character.Abilities.GetSkill<Silence>().SilenceEffectGhostCast(true);
        //character.Abilities.GetSkill<Ghost>().PullingHealthGostTeleport(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<Ghost>().CooldownGhostShotActiveTalent(false);
        character.Abilities.GetSkill<PullingHealth>().SetPullingHealthGhostTalentActive(false);
        //character.Abilities.GetSkill<Silence>().SilenceEffectsOnMinionMagic(false);
        //character.Abilities.GetSkill<Silence>().GhostDeathSilence(false);
        //character.Abilities.GetSkill<Silence>().SilenceEffectGhostCast(false);
        //character.Abilities.GetSkill<Ghost>().PullingHealthGostTeleport(false);
    }
}
