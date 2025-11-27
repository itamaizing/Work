using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScraderAutoAttack : MonoBehaviour
{
    [SerializeField] private SpellMoveTo spellMoveTo;
    [SerializeField] private ScratchClaws scratchClaws;

    public void OnAutoAttackAnimationHitScrader() => spellMoveTo.OnAutoAttackAnimationHit();
    public void OnAutoAttackAnimationEndScrader() => spellMoveTo.OnAutoAttackAnimationEnd();

    public void scraderClawsAnimCast() => scratchClaws.scraderClawsAnimCast();
    public void scraderClawsAnimCastEnd() => scratchClaws.scraderClawsAnimCastEnd();
}
