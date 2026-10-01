using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public static class DescriptionTextProcessor
{
    #region Colors
    public enum ColorPallete
    {
        Y,
        B,
        Air,
        Earth,
        Fire,
        Water,
        Health,
        Mana,
        Energy,
        Rune,
        EvoCD,
    }

    public static readonly Dictionary<ColorPallete, string> ColorMap = new Dictionary<ColorPallete, string>
    {
        {ColorPallete.Y, "#FFFF00"},
        {ColorPallete.B, "#0000FF"},
        {ColorPallete.Air, "#E3DB94"},
        {ColorPallete.Earth, "#DE924F"},
        {ColorPallete.Fire, "#E85A3F"},
        {ColorPallete.Water, "#3FB2E8"},
        {ColorPallete.Health, "#14BD4F"},
        {ColorPallete.Mana, "#2E82E8"},
        {ColorPallete.Energy, "#F0EA65"},
        {ColorPallete.Rune, "#6A9CDE"},
        {ColorPallete.EvoCD, "#9739E3"},
    };
    #endregion

    private static readonly Dictionary<string, string> TagMap = new Dictionary<string, string>
    {
        { "b", "b" },
        { "i", "i" },
        { "u", "u" },
        { "s", "s" },
        { "in", "indent" },
        { "c", "color" },
        { "sz", "size" },
    };

    // Формат: ##[tag][=value]\[content(наш текст)]\##
    private static readonly Regex TagRegex = new Regex(
        @"##(?<tag>[a-z]+)(?<value>[^\\]*?)\\(?<content>(?:[^\\##]|(?!#)#|(?<!#)#|(?!\\)\\)*?)\\##",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase
    );

    public static string Process(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        string previous;
        do
        {
            previous = input;
            input = TagRegex.Replace(input, MatchEvaluator);
        }
        while (input != previous);

        return input;
    }

    private static string MatchEvaluator(Match match)
    {
        string tagName = match.Groups["tag"].Value.ToLower();
        string value = match.Groups["value"].Value;
        string content = match.Groups["content"].Value;

        if (!TagMap.ContainsKey(tagName))
            return match.Value;

        string actualTag = TagMap[tagName];

        string openTag;
        if (string.IsNullOrEmpty(value))
        {
            openTag = $"<{actualTag}>";
        }
        else
        {
            if (actualTag == "color")
            {
                if (System.Enum.TryParse(value.Trim('"', '\'', '='), out ColorPallete color))
                    if (ColorMap.Keys.Contains(color))
                        value = ColorMap[color];
            }
            openTag = $"<{actualTag}={value}>";
        }
        string closeTag = $"</{actualTag}>";

        return openTag + content + closeTag;
    }
}
