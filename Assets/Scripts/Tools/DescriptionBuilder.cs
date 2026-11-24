using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.Localization.SmartFormat;
using UnityEngine.Localization.Settings;

namespace GameDescriptions
{
    public static class DescriptionBuilder
    {
        private static readonly SmartFormatter PlainTextFormatter = CreateFormatter();

        private static SmartFormatter CreateFormatter()
        {
            var formatter = Smart.CreateDefaultSmartFormat();
            DescriptionLocalization.RegisterSource(formatter);
            return formatter;
        }

        public static List<DescriptionRow> BuildRows(DescriptionTable table, DescriptionContext context = null)
        {
            var rows = new List<DescriptionRow>();
            table?.Append(rows, context ?? new DescriptionContext());
            return rows;
        }

        public static string Build(DescriptionTable table, DescriptionContext context = null)
            => DescriptionTable.ToText(BuildRows(table, context));

        public static string FormatText(string text, DescriptionContext context, IFormatProvider culture = null)
        {
            if (string.IsNullOrEmpty(text) || context == null || !context.HasValues || text.IndexOf('{') < 0)
                return text;
            culture ??= context.Culture ?? LocalizationSettings.SelectedLocale?.Identifier.CultureInfo ?? CultureInfo.CurrentCulture;
            return PlainTextFormatter.Format(culture, text, context);
        }

        internal static string FormatTemplate(string template, object values, IFormatProvider culture)
            => PlainTextFormatter.Format(culture, template, values);
    }
}
