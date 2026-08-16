using Backup.ObjectDatabase;
using Backup.ObjectDatabase.ObjectTypes;

namespace Backup.Components.Differences;

public static class DifferenceGenerator
{
    public static List<Difference> TemporaryTestFunction(BackupEntry previous, BackupEntry current)
    {
        return Generate(previous.References[0], current.References[0]);
    }

    public static List<Difference> Generate(ObjectReference previous, ObjectReference current)
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
                DiffTrees(output, previous, current);
                break;
        }
    }

    private static void DiffTrees(List<Difference> output, ObjectReference previous, ObjectReference current)
    {
        Tree previousTree = Database.ReadTree(previous);
        Tree currentTree = Database.ReadTree(current);

        previousTree.PrependRefernces();
        currentTree.PrependRefernces();

        Dictionary<string, ObjectReference> previousReferences = previousTree.References
            .ToDictionary(reference => reference.Name);

        Dictionary<string, ObjectReference> currentReferences = currentTree.References
            .ToDictionary(reference => reference.Name);

        HashSet<string> matchedNames = [];

        foreach (ObjectReference currentReference in currentTree.References)
        {
            if (previousReferences.TryGetValue(currentReference.Name, out ObjectReference? previousReference))
            {
                GenerateInternal(output, currentReference, previousReference);
                matchedNames.Add(currentReference.Name);
                continue;
            }

            if (
                previousTree.TryGetReferenceByPointer(currentReference.Pointer, out ObjectReference? renamedReference) &&
                !matchedNames.Contains(renamedReference!.Name)
            )
            {
                GenerateInternal(output, currentReference, renamedReference);
                matchedNames.Add(renamedReference.Name);
                continue;
            }

            output.Add(Difference.Addition(currentReference));
        }

        foreach (ObjectReference previousReference in previousTree.References)
        {
            if (matchedNames.Contains(previousReference.Name))
                continue;

            if (currentReferences.ContainsKey(previousReference.Name))
                continue;

            if (currentTree.TryGetReferenceByPointer(previousReference.Pointer, out _))
                continue;

            output.Add(Difference.Removal(previousReference));
        }
    }
}