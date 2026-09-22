using UnityEngine;

public class GetomirAutoAttack : MonoBehaviour
{
    [SerializeField] private PowerStrike _powerStrike;
    [SerializeField] private ThrowingBlow _throwingBlow;

    public void PowerStrikeAnimationHit() => _powerStrike.AnimationHit();
    public void ThrowingBlowAnimationHit() => _throwingBlow.AnimationHit();
}

