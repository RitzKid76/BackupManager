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

        if (previous.Name == current.Name)
        {
            if (!previous.Pointer.Equals(current.Pointer))
                HandleChange(output, previous, current);

            return;
        }

        if (!previous.Pointer.Equals(current.Pointer))
        {
            RemoveRecursive(output, previous);
            AddRecursive(output, current);

            return;
        }

        if (previous.Format == ObjectFormat.TREE)
        {
            DiffTrees(output, Database.ReadTree(previous), Database.ReadTree(current));
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
        if (reference.Format != ObjectFormat.TREE)
        {
            output.Add(Difference.Addition(reference));
            return;
        }

        Tree tree = Database.ReadTree(reference);
        tree.PrependRefernces();

        foreach (ObjectReference child in tree.References)
            AddRecursive(output, child);
    }

    private static void RemoveRecursive(List<Difference> output, ObjectReference reference)
    {
        if (reference.Format != ObjectFormat.TREE)
        {
            output.Add(Difference.Removal(reference));
            return;
        }

        Tree tree = Database.ReadTree(reference);
        tree.PrependRefernces();

        foreach (ObjectReference child in tree.References)
            RemoveRecursive(output, child);
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
            if (!previousByName.TryGetValue(current.Name, out ObjectReference? previous))
                continue;

            GenerateInternal(output, previous, current);

            matchedPrevious.Add(previous);
            matchedCurrent.Add(current);
        }

        foreach (ObjectReference current in currentReferences)
        {
            if (matchedCurrent.Contains(current))
                continue;

            List<ObjectReference> candidates = previousReferences
                .Where(previous =>
                    !matchedPrevious.Contains(previous) &&
                    previous.Format == current.Format &&
                    previous.Pointer.Equals(current.Pointer))
                .ToList();

            if (candidates.Count != 1)
            {
                AddRecursive(output, current);
                matchedCurrent.Add(current);

                continue;
            }

            ObjectReference previous = candidates[0];

            GenerateInternal(output, previous, current);

            matchedPrevious.Add(previous);
            matchedCurrent.Add(current);
        }

        foreach (ObjectReference previous in previousReferences)
            if (!matchedPrevious.Contains(previous))
                RemoveRecursive(output, previous);
    }
}