using Mirror;
using UnityEngine;

public class DisciplineShieldBoostBooster : SkillTalentHandler
{
    private bool _enabled;

    private int _disciplineStacks = 0;
    private const int MaxDisciplineStacks = 3;
    private const float DisciplineBoostPercentage = 0.1f;

    public DisciplineShieldBoostBooster(NetworkBehaviour owner) : base(owner) { }

    public override void Enable(bool value)
    {
        _enabled = value;
        if (!value)
            ResetStacks();
    }
    public void OnDisciplineSkillCast()
    {
        if (!_enabled) return;

        if (_disciplineStacks < MaxDisciplineStacks)
            _disciplineStacks++;

        var priestShield = Owner.GetComponent<PriestShield>();
        if (priestShield != null)
        {
            float boost = CalculateBoost();
            priestShield.SetDisciplineShieldBoostValue(boost);
        }
    }

    private float CalculateBoost()
    {
        return _disciplineStacks * DisciplineBoostPercentage;
    }

    public void ResetStacks()
    {
        _disciplineStacks = 0;
        
        var priestShield = Owner.GetComponent<PriestShield>();
        priestShield?.SetDisciplineShieldBoostValue(0f);
    }

    public int CurrentStacks => _disciplineStacks;
}
