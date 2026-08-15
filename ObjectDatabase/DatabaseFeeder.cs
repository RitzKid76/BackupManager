using Backup.ObjectDatabase.ObjectTypes;

namespace Backup.ObjectDatabase;

public static class DatabaseFeeder
{
    public static ObjectReference Feed(string directory, bool ignorePrefix = false)
    {
        string treeName = ignorePrefix
            ? directory[(directory.LastIndexOf('\\') + 1)..]
            : directory;

        Tree tree = new(treeName);

        foreach (string dir in Directory.EnumerateDirectories(directory))
        {
            ObjectReference references = Feed(dir, true);
            tree.AddReference(references);
        }

        foreach (string file in Directory.EnumerateFiles(directory))
        {
            FileInfo fileInfo = new(file);
            
            ObjectReference? reference = Database.WriteFile(fileInfo);
            if (reference is not null)
                tree.AddReference(reference);
        }

        return Database.WriteTree(tree);
    }
}