using System.Text;
using Backup.Components;
using Backup.ObjectDatabase.Hashing;
using Backup.ObjectDatabase.Metadatas;
using Backup.ObjectDatabase.ObjectTypes;

namespace Backup.ObjectDatabase;

public static class GarbageCollector
{
    private static readonly HashSet<Hash> toRemove = [];
    private static readonly HashSet<Hash> seen = [];
    private static readonly List<ObjectReference> missing = [];

    private static readonly HashSet<string> trackedPaths = [];

    public static void Run()
    {
        Logger.Info("garbage collecting...");

        TrashObjects();
        TrashMeta();

        if (missing.Count > 0)
            LogMissing();
    }

    private static void TrashObjects()
    {
        toRemove.Clear();
        seen.Clear();
        missing.Clear();

        trackedPaths.Clear();

        Logger.Info("loading objects...");
        foreach (Hash hash in Database.GetAllPointers())
        {
            Logger.Info($"loading: {hash}");
            toRemove.Add(hash);
        }

        SkipExistingBackupHashes();
        RemoveExtras();
    }

    private static void SkipExistingBackupHashes()
    {
        Logger.Info("tracking objects...");
        foreach (BackupEntry backup in BackupDatabase.GetBackups())
            foreach (ObjectReference reference in backup.References)
                SkipReferenceHashes(reference);
    }

    private static void SkipReferenceHashes(ObjectReference reference)
    {
        if (!toRemove.Remove(reference.Pointer))
        {
            if (seen.Contains(reference.Pointer))
                Logger.Info($"skipping: {reference.Pointer} {reference.FullName}");
            else
                missing.Add(reference);

            return;
        }

        seen.Add(reference.Pointer);

        Logger.Info($"tracking: {reference.Pointer} {reference.FullName}");

        switch (reference.Format)
        {
            case ObjectFormat.TREE:
                SkipTree(reference);
                break;
            case ObjectFormat.BLOB:
                trackedPaths.Add(reference.FullName);
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
        Logger.Info("deleting objects...");
        foreach (Hash hash in toRemove)
            Database.Delete(hash);
    }



    private static void TrashMeta()
    {
        Logger.Info("checking metadata...");
        foreach (PathMetadata metadata in Database.GetAllPathMetas())
        {
            if (!trackedPaths.Contains(metadata.Path))
                Database.DeletePathMetadata(metadata);
            else
                Logger.Info($"skipping: {metadata.CachedPointer} {metadata.Path} path metadata");
        }
    }



    private static void LogMissing()
    {
        StringBuilder builder = new();
        builder.AppendLine("================================ MISSING REFERENCES DETECTED ================================");

        foreach (ObjectReference reference in missing)
            builder.AppendLine($"{reference.Pointer} {reference.FullName}");

        Logger.Log(builder.ToString());
    }
}