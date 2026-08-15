using Backup.Configs;
using Backup.ObjectDatabase;
using Backup.ObjectDatabase.ObjectTypes;

namespace Backup;

public static class BackupDatabase
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

        if (root.References.Count == 0)
            return;

        Database.WriteBackup(root, BackupName());
    }

    public static List<BackupEntry> GetBackups()
    {
        List<BackupEntry> output = [];

        string[] paths = Directory.GetFiles(Config.BackupFolder);
        foreach (string path in paths)
            output.Add(BackupEntry.Parse(path));

        return output;
    }

    private static string BackupName() =>
        DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
}