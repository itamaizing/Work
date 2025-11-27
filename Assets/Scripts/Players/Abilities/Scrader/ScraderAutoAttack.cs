using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScraderAutoAttack : MonoBehaviour
{
    [SerializeField] private SpellMoveScraderTo spellMoveScraderTo;

    public void OnAutoAttackAnimationHitScrader() => spellMoveScraderTo.OnAutoAttackAnimationHit();
    public void OnAutoAttackAnimationEndScrader() => spellMoveScraderTo.OnAutoAttackAnimationEnd();
}
