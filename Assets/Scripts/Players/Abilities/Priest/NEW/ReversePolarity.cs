using System.Collections;
using UnityEngine;

public class ReversePolarity : Skill
{
    [SerializeField] private SparkOfLight sparkOfLight;
    [SerializeField] private FlashOfLight flashOfLight;
    [SerializeField] private Restoration restoration;
    [SerializeField] private PriestShield priestShield;

    protected override bool IsCanCast => true;

    protected override IEnumerator PrepareJob()
    {
        yield break;
    }

    protected override IEnumerator CastJob()
    {
        if (Hero == null || Hero.CharacterState == null || !IsCanCast) yield break;
        
        if (!TryPayCost()) yield break;

        yield return new WaitForSeconds(CastDeley);

        SwitchSpells();
        
        if (Hero.CharacterState.CheckForState(States.ReversePolarity))
        {
            RemoveReversePolarityEffect();
        }
        else
        {
           ApplyReversePolarityEffect();
        }
    }

    private void ApplyReversePolarityEffect()
    {
        Hero.CharacterState.CmdAddState(States.ReversePolarity, -1f, 0, transform.parent.gameObject, "ReversePolarity");
    }

    private void RemoveReversePolarityEffect()
    {
        Hero.CharacterState.CmdRemoveState(States.ReversePolarity);
    }

    private void SwitchSpells()
    {
        sparkOfLight.SwitchMode();
        flashOfLight.SwitchMode();
        restoration.SwitchMode();
        priestShield.SwitchMode();
    }

    protected override void ClearData()
    {
    }
}
