using System.Text.RegularExpressions;

public class RuntimeLine
{
    static readonly Regex TokenRegex = new(@"\{([^}]+)\}", RegexOptions.Compiled);

    public string Text { get; }
    public string Name { get; }

    public RuntimeLine(Line line)
    {
        Text = ConvertTokensToSpriteTags(line.text);
        Name = line.name;
    }

    string ConvertTokensToSpriteTags(string rawText)
    {
        if (string.IsNullOrEmpty(rawText)) return string.Empty;

        return TokenRegex.Replace(rawText, match =>
        {
            string keyword = match.Groups[1].Value.Trim().ToLowerInvariant();
            return $"<sprite name=\"{keyword}\">";
        });
    }
}