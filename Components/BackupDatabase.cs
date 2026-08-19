using Backup.Components.Differences;
using Backup.Configs;
using Backup.ObjectDatabase;

namespace Backup.Components;

public static class BackupDatabase
{
    private static readonly Dictionary<string, BackupEntry> backupsByName = [];
    private static Dictionary<int, BackupEntry> backupsByIndex = [];
    private static Dictionary<string, int> indexMap = [];

    private static readonly Queue<Difference> restoreQueue = [];

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

    public static bool Restore(string backupName, List<string> paths)
    {
        if (!TryGetBackup(backupName, out BackupEntry? backup))
            return false;

        Logger.Log("preparing restore...");

        string latestStateName = LatestStateName(backupName);
        BackupEntry latestState = Generate(latestStateName, true);

        TrackBackup(latestState);
        // using latest as previous to get changes towards restore point
        List<Difference> changesSinceBackup = DifferenceGenerator.FromBackup(latestState, backup!, paths);

        if (changesSinceBackup.Count == 0)
        {
            Delete(latestState, true);

            Logger.Log("no changes to restore.");
            return true;
        }

        foreach (Difference change in changesSinceBackup)
            Logger.Log(change.DiffString());

        bool confirmation = Logger.Confirm($"the changes above will be applied on restore. your current state has been saved as '{latestStateName}'\nare you sure you want to restore to backup '{backupName}'?");
        if (!confirmation)
        {
            Delete(latestState, true);
            return true;
        }

        RestoreDifferences(changesSinceBackup);
        return true;
    }

    private static void RestoreDifferences(List<Difference> differences)
    {
        foreach (Difference difference in differences)
            restoreQueue.Enqueue(difference);

        while (restoreQueue.TryDequeue(out Difference? difference))
            difference.Apply();
    }

    public static BackupEntry Generate(string? backupName = null, bool force = false)
    {
        if (
            backupName is not null &&
            !force &&
            TryGetBackup(backupName, out _)
        ) throw new BackupAlreadyExistsException(backupName);

        (
            IEnumerable<string> paths,
            IEnumerable<BlacklistEntry> blacklist
        ) = PathLoader.Load();

        BackupEntry backup = new(backupName ?? DefaultName());

        foreach (string path in paths)
        {
            ObjectReference? reference = DatabaseFeeder.Feed(path, blacklist);
            if (reference is not null)
                backup.AddReference(reference);
        }

        if (backup.References.Count == 0)
            return backup;

        Logger.Log("finalizing...");

        TrackBackup(backup);

        Database.WriteBackup(backup);
        return backup;
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
            bool confirmation = Logger.Confirm($"are you sure you want to delete backup '{backup.Name}'?");
            if (!confirmation)
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

    private static string DefaultName() =>
        DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

    private static string LatestStateName(string restoring) =>
        $"{DefaultName()}_STATE_BEFORE_RESTORE_TO_{restoring}";
}