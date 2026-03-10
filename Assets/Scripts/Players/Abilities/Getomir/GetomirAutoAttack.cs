using UnityEngine;

public class GetomirAutoAttack : MonoBehaviour
{
    [SerializeField] private SpellMoveGetomirTo _spell;
    [SerializeField] private PowerStrike _powerStrike;

    public void OnAutoAttackAnimationHitScrader() => _spell.OnAutoAttackAnimationHit();
    public void OnAutoAttackAnimationEndScrader() => _spell.OnAutoAttackAnimationEnd();

    public void ScratchClawsAttackAnimationHit() => _powerStrike.AttackAnimationHit();
}

