using Backup.ObjectDatabase.ObjectTypes;

namespace Backup.ObjectDatabase;

public static class DatabaseFeeder
{
    public static ObjectReference? Feed(string path, bool ignorePrefix = false)
    {
        FileAttributes attribute = File.GetAttributes(path);
        if (!attribute.HasFlag(FileAttributes.Directory))
            return FeedFile(path, ignorePrefix);

        return FeedDirectory(path, ignorePrefix);
    }

    private static ObjectReference? FeedFile(string path, bool ignorePrefix) =>
        Database.WriteFile(new(path), !ignorePrefix);

    private static ObjectReference? FeedDirectory(string path, bool ignorePrefix)
    {
        string name = ignorePrefix
            ? Util.ExtractNameFromPath(path)
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

            ObjectReference? reference = Database.WriteFile(fileInfo, !ignorePrefix);
            if (reference is not null)
                tree.AddReference(reference);
        }

        if (tree.References.Count == 0)
            return null;

        return Database.WriteTree(tree);
    }
}