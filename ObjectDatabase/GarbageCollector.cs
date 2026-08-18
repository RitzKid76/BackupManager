using Backup.Components;
using Backup.Components.Differences;
using Backup.ObjectDatabase.Hashing;
using Backup.ObjectDatabase.ObjectTypes;

namespace Backup.ObjectDatabase;

public static class GarbageCollector
{
    private static readonly HashSet<Hash> toRemove = [];

    public static void Run()
    {
        toRemove.Clear();

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
            foreach (Difference difference in backup.Differences)
                SkipDifferenceHashes(difference);
        }
    }

    private static void SkipReferenceHashes(ObjectReference reference)
    {
        toRemove.Remove(reference.Pointer);

        switch (reference.Format)
        {
            case ObjectFormat.TREE:
                SkipTree(reference);
                break;
        }
    }

    private static void SkipDifferenceHashes(Difference difference)
    {
        if (difference.Current is not null)
            toRemove.Remove(difference.Current.Pointer);

        if (difference.Previous is not null)
            toRemove.Remove(difference.Previous.Pointer);
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