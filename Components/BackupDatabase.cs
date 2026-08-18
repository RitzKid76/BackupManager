using Backup.Configs;
using Backup.ObjectDatabase;
using Backup.ObjectDatabase.ObjectTypes;

namespace Backup.Components;

public static class BackupDatabase
{
    private static readonly Dictionary<string, BackupEntry> backupsByName = [];
    private static Dictionary<int, BackupEntry> backupsByIndex = [];
    private static Dictionary<string, int> indexMap = [];

    private static readonly Queue<ObjectReference> restoreQueue = [];

    static BackupDatabase() =>
        PopulateIndexes();

    private static void PopulateIndexes()
    {
        List<BackupEntry> backups = [];

        foreach (string path in Directory.GetFiles(Config.BackupFolder))
            backups.Add(Database.ReadBackup(path));

        backups.Sort((a, b) => b.CreationTime.CompareTo(a.CreationTime));

        for (int i = 0; i < backups.Count; i++)
        {
            BackupEntry backup = backups[i];

            backupsByName[backup.Name] = backup;
            backupsByIndex[i] = backup;
            indexMap[backup.Name] = i;
        }
    }

    private static void TrackBackup(BackupEntry backup)
    {
        Dictionary<int, BackupEntry> newBackupsByIndex = [];
        foreach (KeyValuePair<int, BackupEntry> pair in backupsByIndex)
            newBackupsByIndex[pair.Key + 1] = pair.Value;

        Dictionary<string, int> newIndexMap = [];
        foreach (KeyValuePair<string, int> pair in indexMap)
            newIndexMap[pair.Key] = pair.Value + 1;

        backupsByIndex = newBackupsByIndex;
        indexMap = newIndexMap;

        backupsByName[backup.Name] = backup;
        backupsByIndex[0] = backup;
        indexMap[backup.Name] = 0;

        backup.RegenerateDifference();
    }

    private static void UntrackBackup(BackupEntry backup)
    {
        int backupIndex = indexMap[backup.Name];

        Dictionary<int, BackupEntry> newBackupsByIndex = [];
        foreach (KeyValuePair<int, BackupEntry> pair in backupsByIndex)
        {
            int index = pair.Key;
            if (index == backupIndex)
                continue;

            if (index > backupIndex)
                index--;

            newBackupsByIndex[index] = pair.Value;
        }

        Dictionary<string, int> newIndexMap = [];
        foreach (KeyValuePair<string, int> pair in indexMap)
        {
            int index = pair.Value;
            if (index == backupIndex)
                continue;

            if (index > backupIndex)
                index--;

            newIndexMap[pair.Key] = index;
        }

        backupsByIndex = newBackupsByIndex;
        indexMap = newIndexMap;
        backupsByName.Remove(backup.Name);

        if (TryGetBackup(backupIndex - 1, out BackupEntry? futureBackup))
        {
            futureBackup!.RegenerateDifference();
            Database.WriteBackup(futureBackup);
        }
    }

    public static bool TryGetBackup(string backupName, out BackupEntry? backup) =>
        backupsByName.TryGetValue(backupName, out backup);

    public static bool TryGetBackup(int index, out BackupEntry? backup) =>
        backupsByIndex.TryGetValue(index, out backup);

    public static BackupEntry? GetBackup(int index)
    {
        TryGetBackup(index, out BackupEntry? output);
        return output;
    }

    public static int IndexOf(string backupName)
    {
        if (!indexMap.TryGetValue(backupName, out int index))
            return -1;

        return index;
    }

    public static int Count() =>
        backupsByIndex.Count;

    public static IEnumerable<BackupEntry> GetBackups() =>
        backupsByName.Values.OrderByDescending(b => b.CreationTime);

    public static bool Restore(string backupName)
    {
        if (!TryGetBackup(backupName, out BackupEntry? backup))
            return false;

        RestoreReferences(backup!.References);
        return true;
    }

    public static bool RestoreLatest()
    {
        int backupCount = Count();
        if (backupCount == 0)
            return false;

        TryGetBackup(0, out BackupEntry? backup);

        RestoreReferences(backup!.References);
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

        TrackBackup(backup);

        Database.WriteBackup(backup);
    }

    public static bool Delete(string backupName, bool force = false)
    {
        if (!TryGetBackup(backupName, out BackupEntry? backup))
            return false;

        Delete(backup!, force);
        return true;
    }

    private static bool Delete(BackupEntry backup, bool force = false)
    {
        if (!force)
        {
            Console.Write($"are you sure you want to delete backup '{backup.Name}'? (y/N): ");

            string? answer = Console.ReadLine();
            if (answer != "y" && answer != "yes")
                return true;
        }

        UntrackBackup(backup);

        Database.DeleteBackup(backup);
        return true;
    }

    public static bool Wipe(bool force = false)
    {
        CleanupHandler.BeginWork();

        foreach (string backupName in backupsByName.Keys)
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