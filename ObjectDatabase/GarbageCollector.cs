using Backup.Components;
using Backup.ObjectDatabase.Hashing;
using Backup.ObjectDatabase.ObjectTypes;

namespace Backup.ObjectDatabase;

public static class GarbageCollector
{
    private static readonly HashSet<Hash> toRemove = [];

    public static void Run()
    {
        Logger.Info("garbage collecting...");
        toRemove.Clear();


        Logger.Info("loading objects...");
        foreach (Hash hash in Database.GetAllPointers())
            toRemove.Add(hash);

        SkipExistingBackupHashes();
        RemoveExtras();
    }

    private static void SkipExistingBackupHashes()
    {
        foreach (BackupEntry backup in BackupDatabase.GetBackups())
        {
            foreach (ObjectReference reference in backup.References)
                SkipReferenceHashes(reference);
        }
    }

    private static void SkipReferenceHashes(ObjectReference reference)
    {
        Logger.Info($"tracking: {reference.Pointer} {reference.Name}");
        toRemove.Remove(reference.Pointer);

        switch (reference.Format)
        {
            case ObjectFormat.TREE:
                SkipTree(reference);
                break;
        }
    }

    private static void SkipTree(ObjectReference reference)
    {
        Tree tree = Database.ReadTree(reference);

        foreach (ObjectReference r in tree.References)
            SkipReferenceHashes(r);
    }

    private static void RemoveExtras()
    {
        foreach (Hash hash in toRemove)
            Database.Delete(hash);
    }
}