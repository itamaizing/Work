using UnityEngine;

public class FrostTalent_13 : Talent
{
    [SerializeField] private Character _hero;

    private const float BonusRuneMax = 5f;
    private bool _applied;

    public override void Enter()
    {
        if (_hero == null) _hero = GetComponentInParent<Character>();

        if (_hero == null) return;
        if (_applied) return;

        if (_hero.TryGetResource(ResourceType.Rune, out var resource) && resource is RuneComponent rune)
        {
            rune.AddMax(BonusRuneMax);
            rune.Add(BonusRuneMax);
            _applied = true;
        }
    }

    public override void Exit()
    {
        if (_hero == null)
            _hero = GetComponentInParent<Character>();

        if (_hero == null || !_applied)
            return;

        if (_hero.TryGetResource(ResourceType.Rune, out var resource) && resource is RuneComponent rune)
        {
            rune.AddMax(-BonusRuneMax);
            _applied = false;
        }
    }
}