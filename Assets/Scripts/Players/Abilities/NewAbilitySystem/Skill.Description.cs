public abstract partial class Skill
{
    public virtual TargetLayer DescriptionClickLayers => Targeting?.ClickLayers ?? TargetLayer.None;
    public virtual TargetFaction DescriptionTargetFactions => Targeting?.Faction ?? TargetFaction.None;
    public virtual float DescriptionCastRange => AreaInfo != null ? CastDistance : 0f;
    public virtual float DescriptionArea => AreaInfo?.Area ?? 0f;
    public virtual float DescriptionCooldown => Cooldown?.CooldownTime ?? 0f;
    public virtual float DescriptionPreparingDuration => Hero != null ? PreparingDuration : _preparingDuration;
    public virtual float DescriptionChannelingDuration => Channeling?.CastDuration ?? 0f;

    public virtual float GetDescriptionCost(SkillResourceCost cost)
        => cost.shouldModify && Hero != null && Cost != null
            ? Cost.CalculateModified(cost.value, cost.type) : cost.value;
}
