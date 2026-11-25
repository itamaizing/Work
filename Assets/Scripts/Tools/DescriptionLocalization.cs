using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.SmartFormat;
using UnityEngine.Localization.SmartFormat.Core.Extensions;
using System.Collections.Generic;
using System.Globalization;

namespace GameDescriptions
{
    public static class DescriptionLocalization
    {
        private static readonly Dictionary<string, LocalizedString> TooltipStrings = new();

        public static string Tooltip(string key, params object[] arguments)
        {
            if (!TooltipStrings.TryGetValue(key, out var text))
            {
                text = new LocalizedString("UI.Tooltip", key);
                TooltipStrings.Add(key, text);
            }
            return text.GetLocalizedString(arguments);
        }

        public static string Number(double value, string format = "0.##")
            => value.ToString(format, LocalizationSettings.SelectedLocale?.Identifier.CultureInfo ?? CultureInfo.CurrentCulture);

        public static string GetString(LocalizedString text, object values)
        {
            if (text == null || text.IsEmpty) return string.Empty;
            RegisterSource(LocalizationSettings.StringDatabase.SmartFormatter);
            return values == null ? text.GetLocalizedString() : text.GetLocalizedString(values);
        }

        internal static void RegisterSource(SmartFormatter formatter)
        {
            if (formatter.GetSourceExtension<DescriptionValueSource>() == null)
                formatter.AddExtensions(new DescriptionValueSource());
            if (formatter.GetFormatterExtension<DescriptionValueFormatter>() == null)
                formatter.AddExtensions(new DescriptionValueFormatter());
        }
    }

    public sealed class DescriptionValueSource : ISource
    {
        public bool TryEvaluateSelector(ISelectorInfo selectorInfo)
        {
            if (selectorInfo.CurrentValue is IDescriptionValue description &&
                description.TryGetDescriptionValue(selectorInfo.SelectorText, out var value))
            {
                selectorInfo.Result = value;
                return true;
            }
            return false;
        }
    }
}
