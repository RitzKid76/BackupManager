using Backup.ObjectDatabase;
using Backup.ObjectDatabase.ObjectTypes;

namespace Backup.Components.Differences;

public static class DifferenceGenerator
{
    public static List<Difference> FromBackup(BackupEntry previous, BackupEntry current, List<string>? paths = null)
    {
        List<Difference> output = [];
        DiffReferences(output, previous.References, current.References, r => r.FullName);

        return Filter(output, paths ?? []);
    }

    private static void GenerateInternal(List<Difference> output, ObjectReference previous, ObjectReference current)
    {
        if (previous.Format != current.Format)
        {
            RemoveRecursive(output, previous);
            AddRecursive(output, current);
            return;
        }

        if (previous.FullName == current.FullName)
        {
            if (!previous.Pointer.Equals(current.Pointer))
                HandleChange(output, previous, current);

            return;
        }

        if (previous.Pointer.Equals(current.Pointer))
        {
            output.Add(Difference.Rename(previous, current));
            return;
        }

        RemoveRecursive(output, previous);
        AddRecursive(output, current);
    }

    private static void HandleChange(List<Difference> output, ObjectReference previous, ObjectReference current)
    {
        switch (previous.Format)
        {
            case ObjectFormat.BLOB:
                output.Add(Difference.Change(previous, current));
                break;

            case ObjectFormat.TREE:
                DiffReferences(output, Database.ReadTree(previous).References, Database.ReadTree(current).References, r => r.Name);
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
        foreach (ObjectReference child in tree.References)
            RemoveRecursive(output, child);
    }

    private static void DiffReferences(
        List<Difference> output,
        IEnumerable<ObjectReference> previousReferences,
        IEnumerable<ObjectReference> currentReferences,
        Func<ObjectReference, string> keySelector)
    {
        List<ObjectReference> previousList = previousReferences.ToList();
        List<ObjectReference> currentList = currentReferences.ToList();

        Dictionary<string, ObjectReference> previousByKey = previousList.ToDictionary(keySelector);
        HashSet<ObjectReference> matchedPrevious = [];
        HashSet<ObjectReference> matchedCurrent = [];

        foreach (ObjectReference current in currentList)
        {
            if (!previousByKey.TryGetValue(keySelector(current), out ObjectReference? previous))
                continue;

            GenerateInternal(output, previous, current);

            matchedPrevious.Add(previous);
            matchedCurrent.Add(current);
        }

        foreach (ObjectReference current in currentList)
        {
            if (matchedCurrent.Contains(current))
                continue;

            List<ObjectReference> candidates = previousList
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

        foreach (ObjectReference previous in previousList)
            if (!matchedPrevious.Contains(previous))
                RemoveRecursive(output, previous);
    }

    private static List<Difference> Filter(List<Difference> differences, List<string> paths)
    {
        if (paths.Count == 0)
            return differences;

        return differences
            .Where(difference =>
                (difference.Previous is not null && MatchesAnyPath(difference.Previous.FullName, paths)) ||
                (difference.Current is not null && MatchesAnyPath(difference.Current.FullName, paths)))
            .ToList();
    }

    private static bool MatchesAnyPath(string name, List<string> paths)
    {
        foreach (string path in paths)
            if (MatchesPath(name, path))
                return true;

        return false;
    }

    private static bool MatchesPath(string path, string pattern)
    {
        path = path.Replace('/', '\\');
        pattern = pattern.Replace('/', '\\');

        if (Glob.IsGlob(pattern))
            return Glob.Matches(path, pattern);

        return path.Equals(pattern, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(pattern + '\\', StringComparison.OrdinalIgnoreCase);
    }
}