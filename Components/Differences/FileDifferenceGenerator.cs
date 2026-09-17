namespace Backup.Components.Differences;

public static class FileDifferenceGenerator
{
    public static List<string> DiffContents(string[] previous, string[] current, int contextLines = 3)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(contextLines);

        List<DiffOperation> operations = BuildOperations(previous, current);
        List<DiffHunk> hunks = BuildHunks(operations, contextLines);

        return FormatUnifiedDiff(hunks);
    }

    private static List<string> FormatUnifiedDiff(List<DiffHunk> hunks)
    {
        List<string> output = [];

        foreach (DiffHunk hunk in hunks)
        {
            string marker = $"@@ -{hunk.OldStart},{hunk.OldCount}, +{hunk.NewStart},{hunk.NewCount} @@";
            output.Add(marker);

            foreach (DiffOperation operation in hunk.Operations)
            {
                char prefix = operation.Type switch
                {
                    DiffOperationType.Equal => ' ',
                    DiffOperationType.Insert => '+',
                    DiffOperationType.Delete => '-',
                    _ => throw new ArgumentOutOfRangeException()
                };

                string diffLine = $"{prefix}   {operation.Text}";
                output.Add(diffLine);
            }
        }

        return output;
    }

    private static List<DiffOperation> BuildOperations(string[] oldLines, string[] newLines)
    {
        int oldCount = oldLines.Length;
        int newCount = newLines.Length;

        int[,] lengths = new int[oldCount + 1, newCount + 1];

        for (int oldIndex = oldCount - 1; oldIndex >= 0; oldIndex--)
            for (int newIndex = newCount - 1; newIndex >= 0; newIndex--)
                lengths[oldIndex, newIndex] = oldLines[oldIndex] == newLines[newIndex]
                    ? lengths[oldIndex + 1, newIndex + 1] + 1
                    : Math.Max(lengths[oldIndex + 1, newIndex], lengths[oldIndex, newIndex + 1]);

        List<DiffOperation> operations = [];

        int i = 0;
        int j = 0;

        while (i < oldCount && j < newCount)
        {
            if (oldLines[i] == newLines[j])
            {
                operations.Add(new DiffOperation(DiffOperationType.Equal, oldLines[i]));

                i++;
                j++;

                continue;
            }

            if (lengths[i + 1, j] >= lengths[i, j + 1])
            {
                operations.Add(new DiffOperation(DiffOperationType.Delete, oldLines[i]));
                i++;
            }
            else
            {
                operations.Add(new DiffOperation(DiffOperationType.Insert, newLines[j]));
                j++;
            }
        }

        while (i < oldCount)
        {
            operations.Add(new DiffOperation(DiffOperationType.Delete, oldLines[i]));
            i++;
        }

        while (j < newCount)
        {
            operations.Add(new DiffOperation(DiffOperationType.Insert, newLines[j]));
            j++;
        }

        return operations;
    }

    private static List<DiffHunk> BuildHunks(List<DiffOperation> operations, int contextLines)
    {
        List<DiffHunk> hunks = [];
        int changeStart = -1;
        int changeEnd = -1;

        for (int i = 0; i < operations.Count; i++)
        {
            if (operations[i].Type != DiffOperationType.Equal)
            {
                if (changeStart == -1)
                    changeStart = i;

                changeEnd = i;
            }
            else if (changeStart != -1 && i - changeEnd > contextLines * 2)
            {
                hunks.Add(CreateHunk(operations, changeStart, changeEnd, contextLines));
                changeStart = -1;
                changeEnd = -1;
            }
        }

        if (changeStart != -1)
            hunks.Add(CreateHunk(operations, changeStart, changeEnd, contextLines));

        return hunks;
    }

    private static DiffHunk CreateHunk(List<DiffOperation> operations, int changeStart, int changeEnd, int contextLines)
    {
        int start = Math.Max(0, changeStart - contextLines);
        int end = Math.Min(operations.Count - 1, changeEnd + contextLines);

        List<DiffOperation> hunkOperations = [];

        int oldStart = 1;
        int newStart = 1;

        for (int i = 0; i < start; i++)
        {
            if (operations[i].Type != DiffOperationType.Insert)
                oldStart++;

            if (operations[i].Type != DiffOperationType.Delete)
                newStart++;
        }

        int oldCount = 0;
        int newCount = 0;

        for (int i = start; i <= end; i++)
        {
            DiffOperation operation = operations[i];
            hunkOperations.Add(operation);

            if (operation.Type != DiffOperationType.Insert)
                oldCount++;

            if (operation.Type != DiffOperationType.Delete)
                newCount++;
        }

        return new DiffHunk(oldStart, oldCount, newStart, newCount, hunkOperations);
    }

    public static ConsoleColor? ColorMap(string line) => line[0] switch
    {
        ' ' => ConsoleColor.Gray,
        '+' => ConsoleColor.Green,
        '-' => ConsoleColor.Red,
        '@' => ConsoleColor.Magenta,
        _ => null
    };



    private enum DiffOperationType
    {
        Equal,
        Insert,
        Delete
    }

    private sealed record DiffOperation
    (
        DiffOperationType Type,
        string Text
    );

    private sealed record DiffHunk
    (
        int OldStart,
        int OldCount,
        int NewStart,
        int NewCount,
        List<DiffOperation> Operations
    );
}