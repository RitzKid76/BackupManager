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
        Logger.InfoColorize("garbage collecting...", ConsoleColor.Magenta);

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

        Logger.InfoColorize("loading objects...", ConsoleColor.Magenta);
        foreach (Hash hash in Database.GetAllPointers())
        {
            Logger.InfoColorize($"loading: {hash}", ConsoleColor.DarkGray);
            toRemove.Add(hash);
        }

        SkipExistingBackupHashes();
        RemoveExtras();
    }

    private static void SkipExistingBackupHashes()
    {
        Logger.InfoColorize("tracking objects...", ConsoleColor.Magenta);
        foreach (BackupEntry backup in BackupDatabase.GetBackups())
            foreach (ObjectReference reference in backup.References)
                SkipReferenceHashes(reference);
    }

    private static void SkipReferenceHashes(ObjectReference reference)
    {
        if (!toRemove.Remove(reference.Pointer))
        {
            if (seen.Contains(reference.Pointer))
                Logger.InfoColorize($"skipping: {reference.Pointer} {reference.FullName}", ConsoleColor.DarkGray);
            else
                missing.Add(reference);

            return;
        }

        seen.Add(reference.Pointer);

        Logger.InfoColorize($"tracking: {reference.Pointer} {reference.FullName}", ConsoleColor.DarkGray);

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
        Database.TryReadTree(reference, out Tree? tree);

        foreach (ObjectReference r in tree!.References)
            SkipReferenceHashes(r);
    }

    private static void RemoveExtras()
    {
        Logger.InfoColorize("deleting objects...", ConsoleColor.Magenta);
        foreach (Hash hash in toRemove)
            Database.Delete(hash);
    }



    private static void TrashMeta()
    {
        Logger.InfoColorize("checking metadata...", ConsoleColor.Magenta);
        foreach (PathMetadata metadata in Database.GetAllPathMetas())
        {
            if (!trackedPaths.Contains(metadata.Path))
                Database.DeletePathMetadata(metadata);
            else
                Logger.InfoColorize($"skipping: {metadata.CachedPointer} {metadata.Path} path metadata", ConsoleColor.DarkGray);
        }
    }



    private static void LogMissing()
    {
        List<string> output = [];
        output.Add("================================ MISSING REFERENCES DETECTED ================================");

        foreach (ObjectReference reference in missing)
            output.Add($"{reference.Pointer} {reference.FullName}");

        Logger.Colorize(output, ColorMap);
    }

    private static ConsoleColor? ColorMap(string line)
    {
        if (line.StartsWith('='))
            return ConsoleColor.DarkRed;

        return ConsoleColor.Red;
    }
}