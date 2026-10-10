namespace Backup.Extensions;

public static class Extensions_String
{
    public static string ExtractPathName(this string path) =>
        path[(path.LastIndexOf('\\') + 1)..];

    public static string ExtractPathDirectory(this string path) =>
         path[..path.LastIndexOf('\\')];
}