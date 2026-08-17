using Backup.Extensions;
using Backup.ObjectDatabase.ObjectTypes;

namespace Backup.ObjectDatabase;

public static class DatabaseFeeder
{
    public static ObjectReference? Feed(string path, bool ignorePrefix = false)
    {
        try
        {
            FileAttributes attribute = File.GetAttributes(path);
            if (!attribute.HasFlag(FileAttributes.Directory))
                return FeedFile(path, ignorePrefix);

            return FeedDirectory(path, ignorePrefix);
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

    private static ObjectReference? FeedDirectory(string path, bool ignorePrefix)
    {
        string name = ignorePrefix
            ? path.ExtractPathName()
            : path;

        Tree tree = new(name);

        foreach (string dir in Directory.EnumerateDirectories(path))
        {
            ObjectReference? reference = Feed(dir, true);
            if (reference is not null)
                tree.AddReference(reference);
        }

        foreach (string file in Directory.EnumerateFiles(path))
        {
            FileInfo fileInfo = new(file);

            ObjectReference? reference = Database.WriteFile(fileInfo, false);
            if (reference is not null)
                tree.AddReference(reference);
        }

        if (!tree.References.Any())
            return null;

        return Database.WriteTree(tree);
    }
}