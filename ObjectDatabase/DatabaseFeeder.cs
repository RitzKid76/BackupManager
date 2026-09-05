using Backup.Configs;
using Backup.Extensions;
using Backup.Components.Differences;
using Backup.ObjectDatabase.ObjectTypes;

namespace Backup.ObjectDatabase;

public static class DatabaseFeeder
{
    public static ObjectReference? Feed(string path, IEnumerable<BlacklistEntry> blacklist, bool ignorePrefix = false)
    {
        try
        {
            if (IsBlacklisted(path, blacklist))
                return null;

            FileAttributes attribute = File.GetAttributes(path);

            if (!attribute.HasFlag(FileAttributes.Directory))
                return FeedFile(path, ignorePrefix);

            return FeedDirectory(path, blacklist, ignorePrefix);
        }
        catch (Exception e)
            when (e
                is IOException
                or UnauthorizedAccessException
            )
        {
            return null;
        }
    }

    private static ObjectReference? FeedFile(string path, bool ignorePrefix) =>
        Database.WriteFile(new(path), !ignorePrefix);

    private static ObjectReference? FeedDirectory(string path, IEnumerable<BlacklistEntry> blacklist, bool ignorePrefix)
    {
        string fullName = ignorePrefix
            ? path.ExtractPathName()
            : path;

        Tree tree = new(fullName);

        foreach (string dir in Directory.EnumerateDirectories(path))
        {
            ObjectReference? reference = Feed(dir, blacklist, true);
            if (reference is not null)
                tree.AddReference(reference);
        }

        foreach (string file in Directory.EnumerateFiles(path))
        {
            ObjectReference? reference = Feed(file, blacklist, true);
            if (reference is not null)
                tree.AddReference(reference);
        }

        if (!tree.References.Any())
            return null;

        return Database.WriteTree(tree);
    }

    private static bool IsBlacklisted(string path, IEnumerable<BlacklistEntry> blacklist)
    {
        foreach (BlacklistEntry entry in blacklist)
        {
            if (!IsWithinScope(path, entry.Scope))
                continue;

            string relativePath = entry.Scope.Length == 0
                ? path
                : Path.GetRelativePath(entry.Scope, path);

            if (Glob.Matches(relativePath, entry.Pattern))
                return true;
        }

        return false;
    }

    private static bool IsWithinScope(string path, string scope)
    {
        if (scope.Length == 0)
            return true;

        string fullPath = Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar);

        string fullScope = Path.GetFullPath(scope)
            .TrimEnd(Path.DirectorySeparatorChar);

        return
            fullPath.Equals(fullScope, StringComparison.OrdinalIgnoreCase) ||
            fullPath.StartsWith(fullScope + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}