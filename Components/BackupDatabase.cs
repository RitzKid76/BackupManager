using Backup.Components.Differences;
using Backup.Configs;
using Backup.Extensions;
using Backup.ObjectDatabase;
using Backup.ObjectDatabase.ObjectTypes;

namespace Backup.Components;

public static class BackupDatabase
{
    private static readonly Queue<ObjectReference> restoreQueue = [];

    public static void Generate(string? backupName = null, bool force = false)
    {
        if (
            backupName is not null &&
            !force &&
            TryGetBackup(backupName, out _)
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

        List<BackupEntry> backups = GetBackups();
        if (backups.Count > 0)
        {
            List<Difference> differences = DifferenceGenerator.FromBackup(backups[0], backup);
            foreach (Difference difference in differences)
                backup.AddDifference(difference);
        }

        Database.WriteBackup(backup);
    }

    public static List<BackupEntry> GetBackups()
    {
        List<BackupEntry> output = [];

        foreach (string path in GetBackupNames(true))
            output.Add(Database.ReadBackup(path));

        output.Sort((a, b) => b.CreationTime.CompareTo(a.CreationTime));

        return output;
    }

    private static string[] GetBackupNames(bool keepPrefix)
    {
        string[] paths = Directory.GetFiles(Config.BackupFolder);
        return keepPrefix
            ? paths
            : paths.Select(p => p.ExtractPathName()).ToArray();
    }

    public static bool TryGetBackup(string backupName, out BackupEntry? backup)
    {
        backup = null;
        List<BackupEntry> backups = GetBackups();

        foreach (BackupEntry b in backups)
            if (b.Name == backupName)
            {
                backup = b;
                return true;
            }

        return false;
    }

    public static bool Restore(string backupName)
    {
        if (!TryGetBackup(backupName, out BackupEntry? backup))
            return false;

        RestoreReferences(backup!.References);
        return true;
    }

    public static bool RestoreLatest()
    {
        List<BackupEntry> backups = GetBackups();
        if (backups.Count == 0)
            return false;

        RestoreReferences(backups[0].References);
        return true;
    }

    private static void EnqueueTree(Tree tree)
    {
        tree.PrependRefernces();

        foreach (ObjectReference reference in tree.References)
            restoreQueue.Enqueue(reference);
    }

    private static void RestoreReferences(List<ObjectReference> references)
    {
        foreach (ObjectReference reference in references)
            restoreQueue.Enqueue(reference);

        while (restoreQueue.TryDequeue(out ObjectReference? reference))
        {
            switch (reference.Format)
            {
                case ObjectFormat.TREE:
                    HandleTreeRollback(reference);
                    break;
                case ObjectFormat.BLOB:
                    Database.RestoreFile(reference);
                    break;
            }
        }
    }

    private static void HandleTreeRollback(ObjectReference treeReference)
    {
        Tree tree = Database.ReadTree(treeReference);
        EnqueueTree(tree);
    }

    public static bool Delete(string backupName, bool force = false)
    {
        if (!TryGetBackup(backupName, out BackupEntry? backup))
            return false;

        if (!force)
        {
            Console.Write($"are you sure you want to delete backup '{backupName}'? (y/N): ");

            string? answer = Console.ReadLine();
            if (answer != "y" && answer != "yes")
                return true;
        }

        Database.DeleteBackup(backup!);

        return true;
    }

    public static bool Wipe(bool force = false)
    {
        CleanupHandler.BeginWork();

        foreach (string backupName in GetBackupNames(false))
            if (!Delete(backupName, force))
            {
                CleanupHandler.EndWork();
                return false;
            }

        CleanupHandler.EndWork();
        return true;
    }

    private static string BackupName() =>
        DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
}