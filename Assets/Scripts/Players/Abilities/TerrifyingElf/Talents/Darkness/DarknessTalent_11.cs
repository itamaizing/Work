using UnityEngine;

public class DarknessTalent_11 : Talent
{
    [SerializeField] private Suppression suppression;

    public override void Enter()
    {
        suppression.SuppressionManaAbsorbtion(true);
    }

    public override void Exit()
    {
        suppression.SuppressionManaAbsorbtion(false);
    }
}
