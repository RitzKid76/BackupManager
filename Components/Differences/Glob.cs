using Microsoft.Extensions.FileSystemGlobbing;

namespace Backup.Components.Differences;

public static class Glob
{
    public static bool Matches(string input, string glob)
    {
        input = Normalize(input);
        glob = Normalize(glob);

        string? directory = Path.GetDirectoryName(input);
        if (string.IsNullOrEmpty(directory))
            directory = ".";

        string fileName = Path.GetFileName(input);

        Matcher matcher = new();
        matcher.AddInclude(glob);

        PatternMatchingResult result = matcher.Match(directory, fileName);

        return result.HasMatches;
    }

    public static bool IsGlob(string path) =>
        path.Contains('*') ||
        path.Contains('?');

    private static string Normalize(string path) =>
        path.Replace('\\', '/');
}