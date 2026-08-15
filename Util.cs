namespace Backup;

public static class Util
{
    public static string ExtractNameFromPath(string path) =>
        path[(path.LastIndexOf('\\') + 1)..];
}