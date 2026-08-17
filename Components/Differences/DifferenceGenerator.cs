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
        {
            RemoveRecursive(output, previous);
            AddRecursive(output, current);
            return;
        }

        if (previous.Pointer.Equals(current.Pointer))
        {
            HandleRename(output, previous, current);
            return;
        }

        if (previous.Name != current.Name)
        {
            RemoveRecursive(output, previous);
            AddRecursive(output, current);
            return;
        }

        HandleChange(output, previous, current);
    }

    private static void HandleRename(List<Difference> output, ObjectReference previous, ObjectReference current)
    {
        if (previous.Name == current.Name)
            return;

        if (previous.Format == ObjectFormat.TREE)
        {
            Tree previousTree = Database.ReadTree(previous);
            Tree currentTree = Database.ReadTree(current);

            DiffTrees(output, previousTree, currentTree);
            return;
        }

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

    private static void AddRecursive(List<Difference> output, ObjectReference reference)
    {
        if (reference.Format == ObjectFormat.TREE)
        {
            Tree tree = Database.ReadTree(reference);
            tree.PrependRefernces();

            foreach (ObjectReference child in tree.References)
                AddRecursive(output, child);

            return;
        }

        output.Add(Difference.Addition(reference));
    }

    private static void RemoveRecursive(List<Difference> output, ObjectReference reference)
    {
        if (reference.Format == ObjectFormat.TREE)
        {
            Tree tree = Database.ReadTree(reference);
            tree.PrependRefernces();

            foreach (ObjectReference child in tree.References)
                RemoveRecursive(output, child);

            return;
        }

        output.Add(Difference.Removal(reference));
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

        foreach (ObjectReference current in currentReferences)
        {
            if (previousByName.TryGetValue(current.Name, out ObjectReference? previous))
            {
                GenerateInternal(output, previous, current);
                matchedPrevious.Add(previous);
                matchedCurrent.Add(current);
                continue;
            }

            List<ObjectReference> candidates = previousReferences
                .Where(p => p.Pointer.Equals(current.Pointer) && !matchedPrevious.Contains(p))
                .ToList();

            if (candidates.Count == 1)
            {
                ObjectReference renamed = candidates[0];

                GenerateInternal(output, renamed, current);

                matchedPrevious.Add(renamed);
                matchedCurrent.Add(current);
            }
            else
            {
                AddRecursive(output, current);
                matchedCurrent.Add(current);
            }
        }

        foreach (ObjectReference previous in previousReferences)
        {
            if (matchedPrevious.Contains(previous))
                continue;

            RemoveRecursive(output, previous);
        }
    }
}