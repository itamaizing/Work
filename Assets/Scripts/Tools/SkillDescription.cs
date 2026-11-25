using System.Collections.Generic;
using UnityEngine;
using static GameDescriptions.DescriptionLocalization;

namespace GameDescriptions
{
    public enum DescriptionParameterKind { Cooldown, Cost, Range, Area, Preparing, Channeling, Damage, School, Charges, Counter }

    public readonly struct DescriptionResourceCost
    {
        public ResourceType Resource { get; }
        public string Value { get; }
        public DescriptionResourceCost(ResourceType resource, string value) { Resource = resource; Value = value; }
    }

    public readonly struct SkillDescriptionParameter
    {
        public DescriptionParameterKind Kind { get; }
        public string Label { get; }
        public string Value { get; }
        public string CompactValue { get; }
        public IReadOnlyList<DescriptionResourceCost> Costs { get; }

        public SkillDescriptionParameter(DescriptionParameterKind kind, string label, string value,
            string compactValue, IReadOnlyList<DescriptionResourceCost> costs = null)
        {
            Kind = kind;
            Label = label;
            Value = value;
            CompactValue = compactValue;
            Costs = costs;
        }
    }

    public static class SkillDescription
    {
        private const string ColorOpen = "<color=#53935E>";
        private const string ColorEnd = "</color>";

        public static List<DescriptionRow> Build(Skill skill, bool includeStats = true)
        {
            var rows = new List<DescriptionRow>();
            if (skill == null) return rows;
            Append(rows, skill, skill.AbilityInfoHero, new DescriptionContext(skill.Hero, skill), false, includeStats);
            return rows;
        }

        internal static void Append(List<DescriptionRow> rows, Skill skill, AbilityInfo info,
            DescriptionContext context, bool includeName, bool includeStats = true)
        {
            if (info == null || !context.ActiveAbilities.Add(info)) return;
            try
            {
                context = context.WithSkill(skill);
                if (includeName) DescriptionTable.AddText(rows, info.Name);
                if (includeStats && skill != null)
                {
                    DescriptionTable.AddText(rows, GetStats(skill));
                    rows.Add(DescriptionRow.LineBreak());
                }
                if (info.DescriptionTable != null && info.DescriptionTable.HasEntries)
                {
                    info.DescriptionTable.Append(rows, context);
                    DescriptionTable.AddText(rows, DescriptionBuilder.FormatText(info.SupplementalDescription, context));
                }
                else
                {
                    DescriptionTable.AddText(rows, DescriptionBuilder.FormatText(skill != null ? skill.Description : info.AddingDescription, context));
                    if (!string.IsNullOrWhiteSpace(info.State) || !string.IsNullOrWhiteSpace(info.DescriptionState))
                        DescriptionTable.AddText(rows, $"%%link=State[{info.State}] {info.DescriptionState}");
                }
                if (skill != null)
                {
                    DescriptionTable.AddText(rows, DescriptionBuilder.FormatText(skill.AdditionalDescription, context));
                }
            }
            finally
            {
                context.ActiveAbilities.Remove(info);
            }
        }

        public static string GetStats(Skill skill)
        {
            if (skill == null) return string.Empty;
            var lines = new List<string> { GetTargetingText(skill) };
            foreach (var parameter in GetParameters(skill))
                lines.Add(Tooltip("Format.LabelValue", parameter.Label, parameter.Value));
            return string.Join("\n", lines);
        }

        public static string GetTargetingText(Skill skill)
        {
            if (skill == null) return string.Empty;
            var layers = skill.DescriptionClickLayers;
            var targets = new List<string>();
            if ((layers & TargetLayer.Unit) != 0) targets.Add(Tooltip("Target.Unit.Object"));
            if ((layers & TargetLayer.Ground) != 0) targets.Add(Tooltip("Target.Ground.Object"));
            if ((layers & TargetLayer.Tree) != 0) targets.Add(Tooltip("Target.Tree.Object"));
            string text = Tooltip("Format.LabelValue", Tooltip("CastTarget.Label"), targets.Count > 0 ? string.Join(Tooltip("List.Separator"), targets) : Tooltip("Target.None.Object"));
            if ((layers & TargetLayer.Unit) != 0)
            {
                var factions = new List<string>();
                var allowed = skill.DescriptionTargetFactions;
                if ((allowed & TargetFaction.Self) != 0) factions.Add(Tooltip("Faction.Self.Object"));
                if ((allowed & TargetFaction.Ally) != 0) factions.Add(Tooltip("Faction.Ally.Object"));
                if ((allowed & TargetFaction.Enemy) != 0) factions.Add(Tooltip("Faction.Enemy.Object"));
                if (factions.Count > 0) text += "\n" + Tooltip("Format.LabelValue", Tooltip("Targets.Label"), string.Join(Tooltip("List.Separator"), factions));
            }
            return text;
        }

        public static string GetClickTargetsText(Skill skill)
        {
            var targets = new List<string>();
            var layers = skill.DescriptionClickLayers;
            if ((layers & TargetLayer.Unit) != 0) targets.Add(Tooltip("Target.Unit.Name"));
            if ((layers & TargetLayer.Ground) != 0) targets.Add(Tooltip("Target.Ground.Name"));
            if ((layers & TargetLayer.Tree) != 0) targets.Add(Tooltip("Target.Tree.Name"));
            return targets.Count > 0 ? string.Join(Tooltip("List.Separator"), targets) : Tooltip("Target.None.Name");
        }

        public static string GetTargetFactionsText(Skill skill)
        {
            if ((skill.DescriptionClickLayers & TargetLayer.Unit) == 0) return string.Empty;
            var factions = new List<string>();
            var allowed = skill.DescriptionTargetFactions;
            string Colored(string text, Color color) => $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
            if ((allowed & TargetFaction.Self) != 0) factions.Add(Colored(Tooltip("Faction.Self.Name"), G.Colors.Descriptions.Health));
            if ((allowed & TargetFaction.Ally) != 0) factions.Add(Colored(Tooltip("Faction.Ally.Name"), G.Colors.Chat.Team1));
            if ((allowed & TargetFaction.Enemy) != 0) factions.Add(Colored(Tooltip("Faction.Enemy.Name"), G.Colors.Chat.Team2));
            return string.Join(Tooltip("List.Separator"), factions);
        }

        public static List<SkillDescriptionParameter> GetParameters(Skill skill)
        {
            var parameters = new List<SkillDescriptionParameter>();
            if (skill == null) return parameters;
            void Add(DescriptionParameterKind kind, string value, string compact)
                => parameters.Add(new SkillDescriptionParameter(kind, Tooltip($"Parameter.{kind}.Label"), value, compact));
            void Positive(DescriptionParameterKind kind, float value, string unitKey)
            {
                if (value > 0f) Add(kind, Highlight(Tooltip(unitKey, Number(value))), Number(value));
            }

            if (skill is ICounterSkill)
            {
                string counter = Tooltip("Format.Pair", Number(skill.CurrentCounter), Number(skill.MaxCounter));
                parameters.Add(new SkillDescriptionParameter(DescriptionParameterKind.Counter, skill.CounterSkill, Highlight(counter), counter));
            }
            else
            {
                Add(DescriptionParameterKind.Cooldown, Highlight(Tooltip("Value.Time", Number(skill.DescriptionCooldown))), Number(skill.DescriptionCooldown));
                var costs = new List<string>();
                var compactCosts = new List<DescriptionResourceCost>();
                if (skill.Cost?.Values != null)
                    foreach (var cost in skill.Cost.Values)
                        if (cost != null && cost.showInDescription)
                        {
                            float value = skill.GetDescriptionCost(cost);
                            string prefix = FormatCostLabel(cost.costType);
                            costs.Add(prefix + FormatCost(cost, value));
                            compactCosts.Add(new DescriptionResourceCost(cost.type, prefix + Number(value)));
                        }
                if (skill.Channeling?.Costs != null)
                    foreach (var cost in skill.Channeling.Costs)
                        if (cost != null && cost.showInDescription)
                        {
                            float value = skill.GetDescriptionCost(cost);
                            string period = Tooltip("Value.Time", Number(skill.ManaCostRate));
                            costs.Add(Tooltip("Format.Pair", FormatCost(cost, value), period));
                            compactCosts.Add(new DescriptionResourceCost(cost.type, Tooltip("Format.Pair", Number(value), period)));
                        }
                parameters.Add(new SkillDescriptionParameter(DescriptionParameterKind.Cost, Tooltip("ResourceCost.Label"),
                    costs.Count > 0 ? string.Join("\n", costs) : Highlight("0"), "0", compactCosts));
            }
            Positive(DescriptionParameterKind.Range, skill.DescriptionCastRange, "Value.Distance");
            Positive(DescriptionParameterKind.Area, skill.DescriptionArea, "Value.Distance");
            Positive(DescriptionParameterKind.Preparing, skill.DescriptionPreparingDuration, "Value.Time");
            Positive(DescriptionParameterKind.Channeling, skill.DescriptionChannelingDuration, "Value.Time");
            string damage = GetDamageText(skill);
            if (!string.IsNullOrEmpty(damage))
            {
                string school = GetSchoolNameForDamage(skill);
                Add(DescriptionParameterKind.Damage, string.IsNullOrEmpty(school)
                    ? Tooltip("Value.Damage", Highlight(damage)) : Tooltip("Value.DamageWithSchool", Highlight(damage), school), damage);
            }
            if (skill.Info != null && skill.Info.AbilityForm != AbilityForm.Physical)
                Add(DescriptionParameterKind.School, GetSchoolName(skill), GetSchoolName(skill));
            if (skill.Charges != null && skill.Charges.UsesCharges)
            {
                float recovery = skill.Hero != null ? skill.Charges.CooldownTime : skill.Charges.BaseCooldown;
                Add(DescriptionParameterKind.Charges,
                    Highlight(Tooltip("Format.Pair", Number(skill.Charges.MaxCharges), Tooltip("Value.Time", Number(recovery)))),
                    Tooltip("Format.Pair", Number(skill.Charges.MaxCharges), Number(recovery)));
            }
            return parameters;
        }

        private static string Highlight(string text) => ColorOpen + text + ColorEnd;

        private static string FormatCostLabel(SkillCostType type)
            => type == SkillCostType.Mandatory ? string.Empty : Tooltip($"Cost.{type}.Prefix");

        private static string FormatCost(SkillResourceCost cost, float value)
            => Highlight(Tooltip("Value.ResourceCost", Number(value), Tooltip($"Resource.{cost.type}.CostName")));

        private static string GetDamageText(Skill skill)
        {
            if (skill.BaseDamage is DamageTableRange range)
            {
                float min = skill.Hero != null ? skill.CalculateDamage(range.min) : range.min;
                float max = skill.Hero != null ? skill.CalculateDamage(range.max) : range.max;
                return max > 0 ? $"{Mathf.RoundToInt(min)}–{Mathf.RoundToInt(max)}" : string.Empty;
            }
            float value = skill.BaseDamage is DamageTableSingle single
                ? skill.Hero != null ? skill.CalculateDamage(single.value) : single.value
                : skill.Damage;
            return value > 0 ? Mathf.RoundToInt(value).ToString() : string.Empty;
        }

        private static string GetSchoolNameForDamage(Skill skill)
        {
            var school = skill.Info != null ? skill.Info.School : Schools.None;
            return school is Schools.None or Schools.Physical or Schools.Discipline
                ? string.Empty : Tooltip($"School.{school}.Damage");
        }

        private static string GetSchoolName(Skill skill)
        {
            var school = skill.Info != null ? skill.Info.School : Schools.None;
            return school == Schools.None ? string.Empty : Tooltip($"School.{school}.Genitive");
        }
    }
}
