using UnityEngine;

public class SpisnaciderAutoAttack : MonoBehaviour
{
    [SerializeField] private SpittingAcid _spittingAcid;
    [SerializeField] private ParalyzingTentacles _paralyzingTentacles;

    public void SpittingAcidAttackAnimationHit() => _spittingAcid.AnimCastAcid();
    public void ParalyzingTentaclesAttackAnimationHit() => _paralyzingTentacles.AnimationHit();
}
