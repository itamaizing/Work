using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class HUDPsionicBar : HUDResourceBar
{
    [SerializeField] private Image _attackingFill;
    [SerializeField] private TMP_Text _attackingAmount;
    [SerializeField] private HUDPsiTicks _ticks;

    private float _lastAttackingValue = float.NaN;
    private float _lastAttackingMaximum = float.NaN;

    public override void Bind(Resource resource)
    {
        base.Bind(resource);
        if (resource != null)
            _attackingFill.color = G.Colors.Gameplay.Resources[ResourceType.AttackingPsi].Bar ?? _attackingFill.color;
    }

    protected override void RefreshFill(float current, float max)
    {
        var attackingPsi = (BoundResource as BasePsionicEnergy)?.AttackingPsionicEnergy;
        float attackingMax = attackingPsi != null ? attackingPsi.MaxAttackingPsiEnergy : 0f;
        float attackingValue = attackingPsi != null ? attackingPsi.CurrentValue : 0f;

        float baseWidth = PsionicBarScale.GetFill(current, max, attackingMax, PsionicBarScale.FineWidth);
        float thresholdWidth = PsionicBarScale.GetFill(attackingMax, max, attackingMax, PsionicBarScale.FineWidth);
        float attackingWidth = attackingMax > 0f ? Mathf.Clamp01(attackingValue / attackingMax) * thresholdWidth : 0f;

        var attackingRect = _attackingFill.rectTransform;
        SetFillWidth(Fill, baseWidth);
        SetFillWidth(attackingRect, attackingWidth);

        bool baseInFront = baseWidth < attackingWidth;
        if (baseInFront != (Fill.GetSiblingIndex() > attackingRect.GetSiblingIndex()))
            Fill.SetSiblingIndex(attackingRect.GetSiblingIndex());

        _ticks.SetScale(max, attackingMax);
        if (_lastAttackingValue != attackingValue || _lastAttackingMaximum != attackingMax)
        {
            _lastAttackingValue = attackingValue;
            _lastAttackingMaximum = attackingMax;
            _attackingAmount.enabled = attackingValue > 0f;
            _attackingAmount.SetText("{0:0}/<b>{1:0}</b>", attackingValue, attackingMax);
        }
    }
}
