using UnityEngine;

public class ScraderAutoAttack : MonoBehaviour
{
    [SerializeField] private ScratchClaws scratchClaws;

    public void ScratchClawsAttackAnimationHit() => scratchClaws.AnimCastScratch();
    public void ScratchClawsAttackAnimationHitEnd() => scratchClaws.AnimScratchEnd();
}
