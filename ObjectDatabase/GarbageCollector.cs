using Backup.Components;
using Backup.ObjectDatabase.Hashing;
using Backup.ObjectDatabase.ObjectTypes;

namespace Backup.ObjectDatabase;

public static class GarbageCollector
{
    private static readonly HashSet<Hash> toRemove = [];

    public static void Run()
    {
        foreach (Hash hash in Database.GetAllPointers())
            toRemove.Add(hash);

        SkipExistingBackupHashes();
        RemoveExtras();
    }

    private static void SkipExistingBackupHashes()
    {
        List<BackupEntry> backups = BackupDatabase.GetBackups();
        foreach (BackupEntry backup in backups)
            foreach (ObjectReference reference in backup.References)
                SkipReferenceHashes(reference);
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