using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using GameDescriptions;
using UnityEngine;

public static class DescriptionTextProcessor
{
    public readonly struct LinkStyle
    {
        public string Color { get; }
        public bool Bold { get; }
        public bool Underline { get; }

        public LinkStyle(string color, bool bold = true, bool underline = true)
        {
            Color = color;
            Bold = bold;
            Underline = underline;
        }
    }
    
    public static readonly Dictionary<string, LinkStyle> LinkStyles = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Skill", new LinkStyle("Skill") },
        { "State", new LinkStyle("State") }
    };

    private static readonly HashSet<string> NamedColors = new(StringComparer.OrdinalIgnoreCase)
    {
        "black", "blue", "green", "orange", "purple", "red", "white", "yellow",
        "cyan", "magenta", "grey", "gray", "lightblue", "lime", "maroon", "navy",
        "olive", "silver", "teal", "transparent"
    };

    private static readonly Regex HexColor = new(@"^#[0-9a-fA-F]{6}([0-9a-fA-F]{2})?$", RegexOptions.Compiled);
    private static readonly Regex NumericValue = new(@"^[+-]?(?:\d+(?:\.\d+)?|\.\d+)(?:%|em|px)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    public static string Process(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return DescriptionTable.ToText(ProcessRows(input));
    }

    public static List<DescriptionRow> ProcessRows(string input)
    {
        var parser = new Parser();
        if (!string.IsNullOrEmpty(input)) parser.Parse(input);
        return parser.Rows;
    }

    public static bool TryResolveColor(string value, out string color)
    {
        color = null;
        if (string.IsNullOrWhiteSpace(value)) return false;
        value = value.Trim().Trim('"', '\'');
        if (HexColor.IsMatch(value))
        {
            color = value;
            return true;
        }
        if (G.Colors.TryGetDescriptionColor(value, out var paletteColor))
        {
            color = "#" + (paletteColor.a == 1f
                ? ColorUtility.ToHtmlStringRGB(paletteColor)
                : ColorUtility.ToHtmlStringRGBA(paletteColor));
            return true;
        }
        if (!NamedColors.Contains(value)) return false;
        color = value.ToLowerInvariant();
        return true;
    }

    private static bool TryStyle(string tag, string value, out string open, out string close)
    {
        open = close = string.Empty;
        tag = tag.ToLowerInvariant();
        if (string.IsNullOrEmpty(value))
        {
            if (tag.Length == 0) return false;
            var seen = new HashSet<char>();
            foreach (char style in tag)
            {
                if ("bius".IndexOf(style) < 0) return false;
                if (!seen.Add(style)) continue;
                open += $"<{style}>";
                close = $"</{style}>" + close;
            }
            return true;
        }
        value = value.Trim().Trim('"', '\'');
        switch (tag)
        {
            case "c":
                if (!TryResolveColor(value, out var color)) return false;
                open = $"<color={color}>";
                close = "</color>";
                return true;
            case "in":
            case "sz":
                if (!NumericValue.IsMatch(value)) return false;
                string name = tag == "in" ? "indent" : "size";
                open = $"<{name}={value}>";
                close = $"</{name}>";
                return true;
            case "link":
                if (!LinkStyles.TryGetValue(value, out var link) || !TryResolveColor(link.Color, out color))
                    return false;
                open = (link.Bold ? "<b>" : "") + (link.Underline ? "<u>" : "") + $"<color={color}>";
                close = "</color>" + (link.Underline ? "</u>" : "") + (link.Bold ? "</b>" : "");
                return true;
            default:
                return false;
        }
    }

    private sealed class Parser
    {
        public List<DescriptionRow> Rows { get; } = new();
        private readonly StringBuilder _text = new();
        private readonly List<(string Open, string Close)> _styles = new();
        private bool _hasContent;
        private const int MaxDepth = 128;

        public void Parse(string input)
        {
            ParseRange(input, 0, input.Length, 0);
            Flush();
        }

        private void ParseRange(string input, int start, int end, int depth)
        {
            for (int i = start; i < end;)
            {
                if (input[i] == '\\' && i + 1 < end && "[]\\%".IndexOf(input[i + 1]) >= 0)
                {
                    Append(input.Substring(i + 1, 1));
                    i += 2;
                    continue;
                }
                bool prefix = i + 1 < end && input[i] == '%' && input[i + 1] == '%';
                if (!prefix)
                {
                    Append(input.Substring(i++, 1));
                    continue;
                }
                int nameStart = i + 2;
                int cursor = nameStart;
                while (cursor < end && IsLetter(input[cursor])) cursor++;
                string tag = input.Substring(nameStart, cursor - nameStart);
                if (tag.Equals("hr", StringComparison.OrdinalIgnoreCase) &&
                    (cursor == end || !IsNameCharacter(input[cursor])) &&
                    (cursor == end || input[cursor] != '=' && input[cursor] != '['))
                {
                    Flush();
                    Rows.Add(DescriptionRow.LineBreak());
                    ReopenStyles();
                    i = cursor;
                    continue;
                }
                if (!prefix || cursor == nameStart)
                {
                    Append(input.Substring(i++, 1));
                    continue;
                }
                string value = null;
                if (cursor < end && input[cursor] == '=')
                {
                    int valueStart = ++cursor;
                    while (cursor < end && input[cursor] != '[' && input[cursor] != ']' &&
                           input[cursor] != '\r' && input[cursor] != '\n' &&
                           !(input[cursor] == '%' && cursor + 1 < end && input[cursor + 1] == '%')) cursor++;
                    value = input.Substring(valueStart, cursor - valueStart);
                }
                if (cursor >= end || input[cursor] != '[')
                {
                    Append(input.Substring(i++, 1));
                    continue;
                }
                int closing = FindClosing(input, cursor, end);
                if (closing < 0)
                {
                    Append(input.Substring(i, end - i));
                    return;
                }
                if (depth >= MaxDepth || !TryStyle(tag, value, out var open, out var close))
                    Append(input.Substring(i, closing - i + 1));
                else
                {
                    _styles.Add((open, close));
                    _text.Append(open);
                    ParseRange(input, cursor + 1, closing, depth + 1);
                    _text.Append(close);
                    _styles.RemoveAt(_styles.Count - 1);
                }
                i = closing + 1;
            }
        }

        private static bool IsLetter(char c) => c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z';
        private static bool IsNameCharacter(char c) => IsLetter(c) || char.IsDigit(c) || c == '_';

        private static int FindClosing(string input, int start, int end)
        {
            int balance = 1;
            for (int i = start + 1; i < end; i++)
            {
                if (input[i] == '\\' && i + 1 < end && "[]\\%".IndexOf(input[i + 1]) >= 0) { i++; continue; }
                if (input[i] == '[') balance++;
                if (input[i] == ']' && --balance == 0) return i;
            }
            return -1;
        }

        private void Append(string text)
        {
            _text.Append(text);
            if (!string.IsNullOrWhiteSpace(text)) _hasContent = true;
        }

        private void Flush()
        {
            if (_hasContent)
            {
                for (int i = _styles.Count - 1; i >= 0; i--) _text.Append(_styles[i].Close);
                Rows.Add(new DescriptionRow(_text.ToString()));
            }
            _text.Clear();
            _hasContent = false;
        }

        private void ReopenStyles()
        {
            foreach (var style in _styles) _text.Append(style.Open);
        }
    }
}
