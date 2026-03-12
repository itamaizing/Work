using UnityEngine;

public class GetomirAutoAttack : MonoBehaviour
{
    [SerializeField] private SpellMoveGetomirTo _spell;
    [SerializeField] private PowerStrike _powerStrike;
    [SerializeField] private ThrowingBlow _throwingBlow;

    public void OnAutoAttackAnimationHitGetomir() => _spell.OnAutoAttackAnimationHit();
    public void OnAutoAttackAnimationEndGetomir() => _spell.OnAutoAttackAnimationEnd();

    public void PowerStrikeAnimationHit() => _powerStrike.AnimationHit();
    public void ThrowingBlowAnimationHit() => _throwingBlow.AnimationHit();
}

