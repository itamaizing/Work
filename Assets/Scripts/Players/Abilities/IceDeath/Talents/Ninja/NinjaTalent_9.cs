using UnityEngine;

public class NinjaTalent_9 : Talent
{
    [SerializeField] private Character _hero;

    private const float BonusEnergyMax = 30f;
    private bool _applied;

    public override void Enter()
    {
        if (_hero == null) _hero = GetComponentInParent<Character>();

        if (_hero == null) return;
        if (_applied) return;

        if (_hero.TryGetResource(ResourceType.Energy, out var resource) && resource is Energy energy)
        {
            energy.AddMax(BonusEnergyMax);
            energy.Add(BonusEnergyMax);
            _applied = true;
        }
    }

    public override void Exit()
    {
        if (_hero == null)
            _hero = GetComponentInParent<Character>();

        if (_hero == null || !_applied)
            return;

        if (_hero.TryGetResource(ResourceType.Energy, out var resource) && resource is Energy energy)
        {
            energy.AddMax(-BonusEnergyMax);
            _applied = false;
        }
    }
}