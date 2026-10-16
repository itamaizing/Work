using System.Collections.Generic;
using System.Text;

public static class ChatRichText
{
    private const int MaxTags = 12;

    private static readonly HashSet<string> SimpleTags = new HashSet<string> { "b", "i", "u", "s", "sub", "sup" };

    private static readonly Dictionary<string, string> NamedColors = new Dictionary<string, string>
    {
        { "red", "#FF4D4D" },
        { "green", "#5AC878" },
        { "blue", "#5A96FF" },
        { "yellow", "#FFD64F" },
        { "orange", "#FF9A3C" },
        { "purple", "#B07CFF" },
        { "cyan", "#4FD8E8" },
        { "white", "#FFFFFF" },
        { "gray", "#AAAAAA" }
    };

    public static string Sanitize(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var sb = new StringBuilder(text.Length + 16);
        var open = new List<string>();
        int tags = 0;
        int i = 0;

        while (i < text.Length)
        {
            char c = text[i];
            if (c != '<')
            {
                sb.Append(c);
                i++;
                continue;
            }

            int end = text.IndexOf('>', i + 1);
            if (end > i && TryParseTag(text.Substring(i + 1, end - i - 1), out bool closing, out string name, out string openTag))
            {
                if (!closing)
                {
                    if (tags < MaxTags)
                    {
                        sb.Append(openTag);
                        open.Add(name);
                        tags++;
                        i = end + 1;
                        continue;
                    }
                }
                else
                {
                    int index = open.LastIndexOf(name);
                    if (index >= 0)
                    {
                        for (int k = open.Count - 1; k >= index; k--)
                            sb.Append("</").Append(open[k]).Append('>');
                        open.RemoveRange(index, open.Count - index);
                    }
                    i = end + 1;
                    continue;
                }
            }
            
            sb.Append("<​");
            i++;
        }

        for (int k = open.Count - 1; k >= 0; k--)
            sb.Append("</").Append(open[k]).Append('>');

        return sb.ToString();
    }

    private static bool TryParseTag(string inner, out bool closing, out string name, out string openTag)
    {
        closing = false;
        name = null;
        openTag = null;

        inner = inner.Trim().ToLowerInvariant();
        if (inner.Length == 0) return false;

        if (inner[0] == '/')
        {
            name = inner.Substring(1).Trim();
            closing = true;
            return SimpleTags.Contains(name) || name == "color";
        }

        if (SimpleTags.Contains(inner))
        {
            name = inner;
            openTag = "<" + inner + ">";
            return true;
        }

        if (inner.StartsWith("color="))
        {
            string value = inner.Substring("color=".Length).Trim().Trim('"');
            string hex = NormalizeColor(value);
            if (hex == null) return false;

            name = "color";
            openTag = "<color=" + hex + ">";
            return true;
        }

        return false;
    }

    private static string NormalizeColor(string value)
    {
        if (NamedColors.TryGetValue(value, out string named)) return named;

        if (value.Length < 2 || value[0] != '#') return null;

        string digits = value.Substring(1);
        if (digits.Length != 3 && digits.Length != 6) return null;

        foreach (char ch in digits)
        {
            bool hex = (ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f');
            if (!hex) return null;
        }

        return "#" + digits.ToUpperInvariant();
    }
}