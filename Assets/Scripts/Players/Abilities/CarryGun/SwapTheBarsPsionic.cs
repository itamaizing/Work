using UnityEngine;

public class SwapTheBarsPsionic : MonoBehaviour
{
    [SerializeField] private BasePsionicEnergy basePsionicEnergy;
    [SerializeField] private AttackingPsionicEnergy attackingPsionicEnergy;
 
    [SerializeField] private RectTransform baseEnergySliderTransform;
    [SerializeField] private RectTransform attackingEnergySliderTransform;

    private void Start()
    {
        basePsionicEnergy.OnEnergyChanged += OnEnergyChanged;
        attackingPsionicEnergy.OnEnergyChanged += OnEnergyChanged;

        CompareAndSwap();
    }

    private void OnDestroy()
    {
        basePsionicEnergy.OnEnergyChanged -= OnEnergyChanged;
        attackingPsionicEnergy.OnEnergyChanged -= OnEnergyChanged;
    }

    private void OnEnergyChanged(float value)
    {
        CompareAndSwap();
    }

    private void CompareAndSwap()
    {
        float baseEnergy = basePsionicEnergy.CurrentValue;
        float attackingEnergy = attackingPsionicEnergy.CurrentValue;

        int baseIndex = baseEnergySliderTransform.GetSiblingIndex();
        int attackingIndex = attackingEnergySliderTransform.GetSiblingIndex();
        bool baseInFront = attackingEnergy > baseEnergy;

        if (baseInFront != (baseIndex > attackingIndex))
            baseEnergySliderTransform.SetSiblingIndex(attackingIndex);
    }
}
