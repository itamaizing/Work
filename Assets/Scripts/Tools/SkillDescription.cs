using System.Collections.Generic;
using UnityEngine;

namespace GameDescriptions
{
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
                if (info.DescriptionTable != null && info.DescriptionTable.HasEntries)
                {
                    info.DescriptionTable.Append(rows, context);
                    DescriptionTable.AddText(rows, DescriptionBuilder.FormatText(info.SupplementalDescription, context));
                }
                else
                {
                    DescriptionTable.AddText(rows, DescriptionBuilder.FormatText(skill != null ? skill.Description : info.AddingDescription, context));
                    if (!string.IsNullOrWhiteSpace(info.State) || !string.IsNullOrWhiteSpace(info.DescriptionState))
                        DescriptionTable.AddText(rows, $"<color=#FFFF00>{info.State}</color> {info.DescriptionState}");
                }
                if (skill != null)
                {
                    if (includeStats) DescriptionTable.AddText(rows, GetStats(skill));
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
            string text = string.Empty;
            if (!(skill is ICounterSkill))
            {
                var costs = new List<string>();
                if (skill.Cost?.Values != null)
                    foreach (var cost in skill.Cost.Values)
                        if (cost != null && cost.showInDescription)
                            costs.Add(FormatCostLabel(cost.costType) + FormatCost(skill, cost));
                text = costs.Count == 0 ? $"Затрата: {ColorOpen}0{ColorEnd}" : "Затрата: " + string.Join(" + ", costs);

                if (skill.Channeling?.Costs != null)
                    foreach (var cost in skill.Channeling.Costs)
                        if (cost != null && cost.showInDescription)
                            text += $" + {FormatCost(skill, cost)}/{skill.ManaCostRate} сек";

                switch (skill.Info != null ? skill.Info.AbilityForm : default)
                {

                    case AbilityForm.Magic:
                        text += $" магия {GetSchoolName(skill)}";
                        break;

                    case AbilityForm.Physical:
                        break;

                    default:
                        break;
                }

                string damage = GetDamageText(skill);
                if (!string.IsNullOrEmpty(damage))
                {
                    text += $"\nУрон: {ColorOpen}{damage}{ColorEnd} ед. {GetShoolNameForDamage(skill)}";
                }

                if (skill.PreparingDuration > 0)
                    text += $"\nПодготовка: {ColorOpen}{skill.PreparingDuration} сек{ColorEnd}";

                if (skill.Channeling != null && skill.ChannelingDuration > 0)
                    text += $"\nВыполнение: {ColorOpen}{skill.ChannelingDuration} сек{ColorEnd}";

                if (skill.Cooldown != null && skill.Cooldown.CooldownTime > 0)
                    text += $"\nПерезарядка: {ColorOpen}{skill.Cooldown.CooldownTime} сек{ColorEnd}";

                if (skill.Charges != null && skill.Charges.UsesCharges)
                {
                    float recovery = skill.Hero != null ? skill.Charges.CooldownTime : skill.Charges.BaseCooldown;
                    text += $"\nКол-во Зарядов: {ColorOpen}{skill.Charges.MaxCharges}/{recovery} сек{ColorEnd}";
                }

            }

            else text += $"\n{skill.CounterSkill}: {ColorOpen}{skill.CurrentCounter}/{skill.MaxCounter}{ColorEnd}";

            return text;
        }

        private static string FormatCostLabel(SkillCostType type) => type switch
        {
            SkillCostType.Bonus => "дополнительно: ",
            SkillCostType.Recast => "повторное применение: ",
            SkillCostType.PerSmth => "дополнительная затрата: ",
            _ => string.Empty
        };

        private static string FormatCost(Skill skill, SkillResourceCost cost)
        {
            float value = cost.shouldModify && skill.Hero != null && skill.Cost != null
                ? skill.Cost.CalculateModified(cost.value, cost.type) : cost.value;
            string resource = cost.type switch
            {
                ResourceType.Health => "здоровья",
                ResourceType.Mana => "маны",
                ResourceType.Energy => "энергии",
                ResourceType.Rune => "рун",
                ResourceType.Psionic => "псионики",
                ResourceType.AttackingPsi => "атакующей псионики",
                ResourceType.CooldownEnergy => "энергии перезарядки",
                _ => cost.type.ToString()
            };
            return $"{ColorOpen}{value} ед. {resource}{ColorEnd}";
        }

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

        private static string GetShoolNameForDamage(Skill skill)
        {
            switch (skill.Info != null ? skill.Info.School : Schools.None)
            {
                case Schools.Light:
                    return "светом";

                case Schools.Dark:
                    return "тьмой";

                case Schools.Fire:
                    return "огнем";

                case Schools.Water:
                    return "водой";

                case Schools.Air:
                    return "воздухом";

                case Schools.Earth:
                    return "землей";

                case Schools.Physical:
                    return "";

                case Schools.Discipline:
                    return "";

                case Schools.None:
                    return "";

                default:
                    return "";
            }
        }

        private static string GetSchoolName(Skill skill)
        {
            switch (skill.Info != null ? skill.Info.School : Schools.None)
            {
                case Schools.Light:
                    return "света";
                case Schools.Dark:
                    return "тьмы";
                case Schools.Fire:
                    return "огня";
                case Schools.Water:
                    return "воды";
                case Schools.Air:
                    return "воздуха";
                case Schools.Earth:
                    return "земли";
                case Schools.Physical:
                    return "";
                case Schools.Discipline:
                    return "";
                case Schools.None:
                    return "";
                default:
                    return "";
            }
        }
    }
}

