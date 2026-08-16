namespace Backup;

public static class Util
{
    public static string ExtractNameFromPath(string path) =>
        path[(path.LastIndexOf('\\') + 1)..];

    public static string ExtractDirectoryFromPath(string path) =>
         path[..path.LastIndexOf('\\')];

    public static string PadString(string s, int count, char padChar = ' ')
    {
        int current = s.Length;
        int remaining = count - current;

        if (remaining <= 0)
            return s;

        string padding = new(padChar, remaining);

        return s + padding;
    }
}