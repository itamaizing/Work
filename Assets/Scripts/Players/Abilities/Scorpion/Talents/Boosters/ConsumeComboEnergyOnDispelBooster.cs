using Mirror;

public class ConsumeComboEnergyOnDispelBooster : SkillTalentHandler
{
    private bool _enabled;

    private const float EnergyPercentPerEffect = 0.3f;

    public bool Enabled => _enabled;

    public ConsumeComboEnergyOnDispelBooster(NetworkBehaviour owner) : base(owner) { }

    public override void Enable(bool value) => _enabled = value;

    public void ApplyEnergyForOneEffect()
    {
        var skill = Owner as ConsumeCombo_Scorpion;
        if (skill?.Hero == null) return;

        if (skill.Hero.TryGetResource(ResourceType.Energy) is Resource energy)
        {
            float restoreAmount = energy.MaxValue * EnergyPercentPerEffect;

            energy.Add(restoreAmount);
        }
    }
}