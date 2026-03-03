using UnityEngine;

public class SpisnaciderAutoAttack : MonoBehaviour
{
    [SerializeField] private SpellMoveSpisnaciderTo _spellMoveSpisnacider;
    [SerializeField] private SpittingAcid _spittingAcid;

    public void OnAutoAttackAnimationHitSpisnacider() => _spellMoveSpisnacider.OnAutoAttackAnimationHit();
    public void OnAutoAttackAnimationEndSpisnacider() => _spellMoveSpisnacider.OnAutoAttackAnimationEnd();

    public void ScratchClawsAttackAnimationHit() => _spittingAcid.TriggerAnimation();
}
