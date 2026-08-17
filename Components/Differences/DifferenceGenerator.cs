using Backup.ObjectDatabase;
using Backup.ObjectDatabase.ObjectTypes;

namespace Backup.Components.Differences;

public static class DifferenceGenerator
{
    public static List<Difference> FromBackup(BackupEntry previous, BackupEntry current)
    {
        Tree previousTree = new(previous.References);
        Tree currentTree = new(current.References);

        List<Difference> output = [];
        DiffTrees(output, previousTree, currentTree);

        return output;
    }

    public static List<Difference> FromReference(ObjectReference previous, ObjectReference current)
    {
        List<Difference> output = [];

        GenerateInternal(output, previous, current);

        return output;
    }

    private static void GenerateInternal(List<Difference> output, ObjectReference previous, ObjectReference current)
    {
        if (previous.Format != current.Format)
            throw new ObjectFormatMismatchException(previous.Format, current.Format);

        if (previous.Pointer.Equals(current.Pointer))
        {
            HandleRename(output, previous, current);
            return;
        }

        if (previous.Name != current.Name)
        {
            output.Add(Difference.Removal(previous));
            output.Add(Difference.Addition(current));

            return;
        }

        HandleChange(output, previous, current);
    }

    private static void HandleRename(List<Difference> output, ObjectReference previous, ObjectReference current)
    {
        if (previous.Name != current.Name)
            output.Add(Difference.Rename(previous, current));
    }

    private static void HandleChange(List<Difference> output, ObjectReference previous, ObjectReference current)
    {
        switch (previous.Format)
        {
            case ObjectFormat.BLOB:
                output.Add(Difference.Change(previous, current));
                break;

            case ObjectFormat.TREE:
                DiffTrees(output, Database.ReadTree(previous), Database.ReadTree(current));
                break;
        }
    }

    private static void DiffTrees(List<Difference> output, Tree previousTree, Tree currentTree)
    {
        previousTree.PrependRefernces();
        currentTree.PrependRefernces();

        List<ObjectReference> previousReferences = previousTree.References.ToList();
        List<ObjectReference> currentReferences = currentTree.References.ToList();

        Dictionary<string, ObjectReference> previousByName = previousReferences.ToDictionary(r => r.Name);
        HashSet<ObjectReference> matchedPrevious = [];
        HashSet<ObjectReference> matchedCurrent = [];

        // First pass: match by exact name
        foreach (ObjectReference current in currentReferences)
        {
            if (previousByName.TryGetValue(current.Name, out ObjectReference? previous))
            {
                GenerateInternal(output, previous, current);
                matchedPrevious.Add(previous);
                matchedCurrent.Add(current);
                continue;
            }

            // No name match: try to detect rename by content hash
            List<ObjectReference> candidates = previousReferences
                .Where(p => p.Pointer.Equals(current.Pointer) && !matchedPrevious.Contains(p))
                .ToList();

            if (candidates.Count == 1)
            {
                // Unique candidate -> safe to treat as rename
                ObjectReference renamed = candidates[0];
                GenerateInternal(output, renamed, current);
                matchedPrevious.Add(renamed);
                matchedCurrent.Add(current);
            }
            else
            {
                // Ambiguous or no candidate -> treat current as new file
                output.Add(Difference.Addition(current));
                matchedCurrent.Add(current);
            }
        }

        // Second pass: any unmatched previous reference is a removal
        foreach (ObjectReference previous in previousReferences)
        {
            if (matchedPrevious.Contains(previous))
                continue;

            output.Add(Difference.Removal(previous));
        }
    }
}