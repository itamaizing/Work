using DG.Tweening;
using System.Collections;
using UnityEngine;

public class ThrowingBlow : SkillCreatureCarryGun
{
    [SerializeField] private float knockbackDistance = 6f;
    [SerializeField] private float knockbackDuration = 0.35f;

    private const string SpeedSource = "ThrowingBlowSpeed";

    protected override string AnimationTrigger => "AttackGetomir";

    protected override IEnumerator CastJob()
    {
        if (_moveCreature != null)
            _moveCreature.SetSpeedModifier(SpeedSource, 1.3f);

        yield return base.CastJob();

        if (_moveCreature != null) _moveCreature.RemoveSpeedModifier(SpeedSource);
    }

    protected override void ApplySkillEffect(Character target)
    {
        if (target == null)
            return;

        Vector3 dir = (target.transform.position - transform.position).normalized;
        dir.y = 0;

        Vector3 endPos = target.transform.position + dir * knockbackDistance;

        target.transform.DOMove(endPos, knockbackDuration)
            .SetEase(Ease.OutQuad);
    }
}