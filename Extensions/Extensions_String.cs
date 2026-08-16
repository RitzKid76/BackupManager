namespace Backup.Extensions;

public static class Extensions_String
{
    public static string ExtractPathName(this string path) =>
        path[(path.LastIndexOf('\\') + 1)..];

    public static string ExtractPathDirectory(this string path) =>
         path[..path.LastIndexOf('\\')];

    public static string PadString(this string s, int count, char padChar = ' ')
    {
        int current = s.Length;
        int remaining = count - current;

        if (remaining <= 0)
            return s;

        string padding = new(padChar, remaining);

        return s + padding;
    }
}