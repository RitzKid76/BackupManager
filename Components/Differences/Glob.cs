using System.Text;
using System.Text.RegularExpressions;

namespace Backup.Components.Differences;

public static class Glob
{
    public static bool Matches(string input, string pattern)
    {
        input = Normalize(input);
        pattern = Normalize(pattern);

        if (!IsGlob(pattern))
            return input.Contains(pattern, StringComparison.OrdinalIgnoreCase);

        string regexPattern = ConvertToRegex(pattern);
        return Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase);
    }

    private static bool IsGlob(string path) =>
        path.Contains('*') ||
        path.Contains('?');

    private static string Normalize(string path) =>
        path.Replace('\\', '/');

    private static string ConvertToRegex(string glob)
    {
        StringBuilder regex = new();

        foreach (char character in glob)
        {
            switch (character)
            {
                case '*':
                    regex.Append(".*");
                    break;

                case '?':
                    regex.Append('.');
                    break;

                default:
                    regex.Append(Regex.Escape(character.ToString()));
                    break;
            }
        }

        return regex.ToString();
    }
}