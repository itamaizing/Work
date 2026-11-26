public abstract partial class Skill
{
    private readonly System.Collections.Generic.List<DescriptionSupplement> _descriptionSupplements = new();

    private sealed class DescriptionSupplement
    {
        public object Source;
        public GameDescriptions.DescriptionEntry Entry;
        public IDescriptionValue Values;
    }

    public void SetDescriptionSupplement(object source, bool active, GameDescriptions.DescriptionEntry entry,
        IDescriptionValue values = null)
    {
        if (source == null) throw new System.ArgumentNullException(nameof(source));
        _descriptionSupplements.RemoveAll(item => ReferenceEquals(item.Source, source));
        if (!active) return;
        if (entry == null)
        {
            UnityEngine.Debug.LogWarning($"Missing description entry for {source.GetType().Name} on {name}.", this);
            return;
        }
        _descriptionSupplements.Add(new DescriptionSupplement { Source = source, Entry = entry, Values = values });
    }

    internal void AppendDescriptionSupplements(System.Collections.Generic.List<GameDescriptions.DescriptionRow> rows,
        GameDescriptions.DescriptionContext context)
    {
        foreach (var item in _descriptionSupplements)
            item.Entry.Append(rows, item.Values != null ? context.WithValues(item.Values) : context);
    }

    public virtual TargetLayer DescriptionClickLayers => Targeting?.ClickLayers ?? TargetLayer.None;
    public virtual TargetFaction DescriptionTargetFactions => Targeting?.Faction ?? TargetFaction.None;
    public virtual float DescriptionCastRange => AreaInfo != null ? CastDistance : 0f;
    public virtual float DescriptionArea => AreaInfo?.Area ?? 0f;
    public virtual float DescriptionCooldown => Cooldown?.CooldownTime ?? 0f;
    public virtual float DescriptionPreparingDuration => Hero != null ? PreparingDuration : _preparingDuration;
    public virtual float DescriptionChannelingDuration => Channeling?.CastDuration ?? 0f;

    public virtual float GetDescriptionCost(SkillResourceCost cost) => cost.shouldModify && Hero != null && Cost != null
            ? Cost.CalculateModified(cost.value, cost.type) : cost.value;
}
