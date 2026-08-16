using Backup.Configs;
using Backup.ObjectDatabase;
using Backup.ObjectDatabase.ObjectTypes;

namespace Backup;

public static class BackupDatabase
{
    private static readonly Queue<ObjectReference> rollbackQueue = [];

    public static void Generate(string? backupName = null, bool force = false)
    {
        if (
            backupName is not null &&
            !force &&
            HasBackup(backupName)
        ) throw new BackupAlreadyExistsException(backupName);

        List<string> paths = PathLoader.Load();
        BackupEntry backup = new(backupName ?? BackupName());

        foreach (string path in paths)
        {
            ObjectReference? reference = DatabaseFeeder.Feed(path);
            if (reference is not null)
                backup.AddReference(reference);
        }

        if (backup.References.Count == 0)
            return;

        Database.WriteBackup(backup);
    }

    public static List<BackupEntry> GetBackups()
    {
        List<BackupEntry> output = [];

        foreach (string path in Directory.EnumerateFiles(Config.BackupFolder))
            output.Add(Database.ReadBackup(path));

        output.Sort((a, b) => b.CreationTime.CompareTo(a.CreationTime));

        return output;
    }

    private static bool HasBackup(string backupName)
    {
        List<BackupEntry> backups = GetBackups();

        foreach (BackupEntry backup in backups)
            if (backup.Name == backupName)
                return true;

        return false;
    }

    public static void Restore(BackupEntry backup)
    {
        foreach (ObjectReference reference in backup.References)
            rollbackQueue.Enqueue(reference);

        InternalRestore();
    }

    private static void EnqueueTree(Tree tree)
    {
        foreach (ObjectReference reference in tree.References)
        {
            reference.PrependPath(tree.Name);
            rollbackQueue.Enqueue(reference);
        }
    }

    private static void InternalRestore()
    {
        while (rollbackQueue.TryDequeue(out ObjectReference? reference))
        {
            switch (reference.Format)
            {
                case ObjectFormat.TREE:
                    HandleTreeRollback(reference);
                    break;
                case ObjectFormat.BLOB:
                    Database.ReadFile(reference);
                    break;
            }
        }
    }

    private static void HandleTreeRollback(ObjectReference treeReference)
    {
        Tree tree = Database.ReadTree(treeReference);
        EnqueueTree(tree);
    }

    private static string BackupName() =>
        DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
}