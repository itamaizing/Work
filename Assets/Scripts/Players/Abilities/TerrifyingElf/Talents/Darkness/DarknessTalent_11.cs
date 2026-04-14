using UnityEngine;

public class DarknessTalent_11 : Talent
{
    [SerializeField] private Suppression suppression;
    [SerializeField] private TerrifyingElfAura terrifying;

    public override void Enter()
    {
        suppression.SuppressionManaAbsorbtion(true);
        terrifying.SpellAddInnerDarkness(true);
    }

    public override void Exit()
    {
        suppression.SuppressionManaAbsorbtion(false);
        terrifying.SpellAddInnerDarkness(false);
    }
}
