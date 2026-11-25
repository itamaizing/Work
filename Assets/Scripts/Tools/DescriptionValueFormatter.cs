using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.Localization;
using UnityEngine.Localization.SmartFormat.Core.Extensions;

namespace GameDescriptions
{
    public readonly struct DescriptionUnavailable { }

    public readonly struct DescriptionDuration
    {
        public double Seconds { get; }
        public DescriptionDuration(double seconds) => Seconds = seconds;
    }

    public interface IDescriptionValueFormatter
    {
        bool TryFormat(object value, string format, DescriptionContext context,
            IFormatProvider culture, out string result);
    }

    public sealed class DescriptionValueFormatter : IFormatter
    {
        public string[] Names { get; set; } = new[] { "", "description", "desc", "seconds" };

        public static IList<IDescriptionValueFormatter> Handlers { get; } = new List<IDescriptionValueFormatter>
        {
            new DamageDescriptionFormatter(), new SkillValueFormatter(), new StateValueFormatter()
        };

        public bool TryEvaluateFormat(IFormattingInfo info)
        {
            if (info.CurrentValue is DescriptionUnavailable)
            {
                info.Write("—");
                return true;
            }
            if (info.Format != null && info.Format.HasNested) return false;
            var arguments = info.FormatDetails.OriginalArgs;
            var context = arguments.Count > 0 ? arguments[0] as DescriptionContext : null;
            string format = info.Placeholder.FormatterName == "seconds"
                ? "seconds" : !string.IsNullOrEmpty(info.FormatterOptions)
                    ? info.FormatterOptions : info.Format?.GetLiteralText();
            var culture = info.FormatDetails.Provider ?? CultureInfo.CurrentCulture;
            foreach (var handler in Handlers)
            {
                if (!handler.TryFormat(info.CurrentValue, format, context, culture, out var result)) continue;
                info.Write(result);
                return true;
            }

            if (info.CurrentValue is DescriptionDuration duration)
            {
                info.Write($"<b>{DescriptionLocalization.Tooltip("Value.Time", duration.Seconds.ToString("0.##", culture))}</b>");
                return true;
            }

            bool seconds = format == "seconds";
            if ((context == null && !seconds) || !IsNumber(info.CurrentValue)) return false;
            string number = ((IFormattable)info.CurrentValue).ToString(seconds ? "0.##" : format, culture);
            string text = seconds ? DescriptionLocalization.Tooltip("Value.Time", number) : number;
            info.Write($"<b>{text}</b>");
            return true;
        }

        private static bool IsNumber(object value)
        {
            if (value == null || value.GetType().IsEnum) return false;
            var code = Type.GetTypeCode(value.GetType());
            return code >= TypeCode.SByte && code <= TypeCode.Decimal;
        }

    }

    public sealed class DamageDescriptionFormatter : IDescriptionValueFormatter
    {
        public static LocalizedString Template { get; set; }

        public bool TryFormat(object value, string format, DescriptionContext context,
            IFormatProvider culture, out string result)
        {
            result = null;
            float min, max;
            DamageType type;
            Schools school = context?.Skill != null && context.Skill.Info != null ? context.Skill.Info.School : Schools.None;
            switch (value)
            {
                case DamageTableSingle single:
                    min = max = Modify(single.value, context);
                    type = single.Type;
                    break;
                case DamageTableRange range:
                    min = Modify(range.min, context);
                    max = Modify(range.max, context);
                    type = range.Type;
                    break;
                case Damage damage:
                    min = max = damage.Value;
                    type = damage.Type;
                    school = damage.School;
                    break;
                case DamageTable other:
                    result = other.GetDescriptionString();
                    return true;
                default:
                    return false;
            }

            string amount = min == max ? min.ToString("0.##", culture)
                : $"{min.ToString("0.##", culture)}-{max.ToString("0.##", culture)}";
            var data = new DamageArguments
            {
                Amount = amount, DamageType = TypeName(type),
                Min = min, Max = max, Last = max, School = school.ToString()
            };
            string text;
            if (Template != null && !Template.IsEmpty)
                text = DescriptionLocalization.GetString(Template, data);
            else
            {
                text = string.IsNullOrEmpty(data.DamageType)
                    ? DescriptionLocalization.Tooltip("Value.UntypedDamage", amount)
                    : DescriptionLocalization.Tooltip("Value.TypedDamage", amount, data.DamageType);
            }
            result = $"<b>{text}</b>";
            return true;
        }

        private static float Modify(float value, DescriptionContext context)
            => context?.Skill != null && context.Skill.Hero != null ? context.Skill.CalculateDamage(value) : value;

        private static string TypeName(DamageType type) => type switch
        {
            DamageType.Physical => DescriptionLocalization.Tooltip("DamageType.Physical.Name"),
            DamageType.Magical => DescriptionLocalization.Tooltip("DamageType.Magical.Name"),
            DamageType.Both => DescriptionLocalization.Tooltip("DamageType.Both.Name"),
            DamageType.Inner => DescriptionLocalization.Tooltip("DamageType.Inner.Name"),
            _ => string.Empty
        };

        public sealed class DamageArguments
        {
            public string Amount { get; set; }
            public string DamageType { get; set; }
            public float Min { get; set; }
            public float Max { get; set; }
            public float Last { get; set; }
            public string School { get; set; }
        }
    }

    public sealed class SkillValueFormatter : IDescriptionValueFormatter
    {
        public bool TryFormat(object value, string format, DescriptionContext context,
            IFormatProvider culture, out string result)
        {
            result = null;
            if (!(value is Skill skill)) return false;
            if (skill == null) { result = string.Empty; return true; }
            if (string.Equals(format, "full", StringComparison.OrdinalIgnoreCase))
            {
                var rows = new List<DescriptionRow>();
                SkillDescription.Append(rows, skill, skill.AbilityInfoHero,
                    context ?? new DescriptionContext(skill: skill), true);
                result = DescriptionTable.ToText(rows);
            }
            else
            {
                string name = skill.AbilityInfoHero != null ? skill.AbilityInfoHero.Name : skill.GetType().Name;
                result = $"%%link=Skill[{name}]";
            }
            return true;
        }
    }

    public sealed class StateValueFormatter : IDescriptionValueFormatter
    {
        public bool TryFormat(object value, string format, DescriptionContext context,
            IFormatProvider culture, out string result)
        {
            result = null;
            if (!(value is StateData_SO state)) return false;
            if (state == null) { result = string.Empty; return true; }
            if (string.Equals(format, "full", StringComparison.OrdinalIgnoreCase))
            {
                var rows = new List<DescriptionRow>();
                StateDescription.Append(rows, state, context);
                result = DescriptionTable.ToText(rows);
            }
            else
            {
                context = StateDescription.GetContext(state, context);
                if (!context.ActiveStates.Add(state)) { result = string.Empty; return true; }
                try { result = $"%%link=State[{DescriptionLocalization.GetString(state.Name, context)}]"; }
                finally { context.ActiveStates.Remove(state); }
            }
            return true;
        }
    }
}
