using Backup.Configs;
using Backup.ObjectDatabase;
using Backup.ObjectDatabase.ObjectTypes;

namespace Backup;

public static class BackupGenerator
{
    public static void Generate()
    {
        List<string> paths = PathLoader.Load();
        Tree root = new("root");

        foreach (string path in paths)
        {
            ObjectReference? reference = DatabaseFeeder.Feed(path);
            if (reference is not null)
                root.AddReference(reference);
        }

        Database.WriteBackup(root, BackupName());
    }

    private static string BackupName() =>
        DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
}