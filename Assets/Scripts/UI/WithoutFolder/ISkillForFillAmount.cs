public interface ISkillForFillAmount
{
    float CooldownTime { get; }
    float RemainingCooldownTime { get; }
    bool IsInCooldown { get; }
}
