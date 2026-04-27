using Gangdollarff;

public class TelekinesisDamageTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<Telekinesis>().IsApplyDamageTalent(true);
        character.Abilities.GetSkill<CounterSpell>().IsApplyDamageTalent(true);
        character.Abilities.GetSkill<SpellThiefSkill>().IsApplyDamageTalent(true);
        character.Abilities.GetSkill<SchoolSolvent>().IsApplyDamageTalent(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<Telekinesis>().IsApplyDamageTalent(false);
        character.Abilities.GetSkill<CounterSpell>().IsApplyDamageTalent(false);
        character.Abilities.GetSkill<SpellThiefSkill>().IsApplyDamageTalent(false);
        character.Abilities.GetSkill<SchoolSolvent>().IsApplyDamageTalent(false);
    }
}
