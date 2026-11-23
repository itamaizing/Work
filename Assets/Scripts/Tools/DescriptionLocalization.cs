using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.SmartFormat;
using UnityEngine.Localization.SmartFormat.Core.Extensions;

namespace GameDescriptions
{
    public static class DescriptionLocalization
    {
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
