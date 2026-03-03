using UnityEngine;

public class ScraderAutoAttack : MonoBehaviour
{
    [SerializeField] private SpellMoveScraderTo spellMoveScraderTo;
    [SerializeField] private ScratchClaws scratchClaws;

    public void OnAutoAttackAnimationHitScrader() => spellMoveScraderTo.OnAutoAttackAnimationHit();
    public void OnAutoAttackAnimationEndScrader() => spellMoveScraderTo.OnAutoAttackAnimationEnd();

    public void ScratchClawsAttackAnimationHit() => scratchClaws.TriggerAnimation();
}
