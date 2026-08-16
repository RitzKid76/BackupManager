using System.Text;
using Backup.ObjectDatabase;

namespace Backup.Components.Differences;

public class Difference
{
    public DifferenceType Type { get; }
    public ObjectReference? Current { get; }
    public ObjectReference? Previous { get; }

    private Difference(DifferenceType type, ObjectReference? previous, ObjectReference? current)
    {
        Type = type;
        Previous = previous;
        Current = current;
    }

    public static Difference Rename(ObjectReference previous, ObjectReference current) =>
        new(DifferenceType.RENAME, previous, current);

    public static Difference Addition(ObjectReference current) =>
        new(DifferenceType.ADDITION, null, current);

    public static Difference Removal(ObjectReference previous) =>
        new(DifferenceType.REMOVAL, previous, null);

    public static Difference Change(ObjectReference previous, ObjectReference current) =>
        new(DifferenceType.CHANGE, previous, current);

    public override string ToString()
    {
        StringBuilder output = new();

        output.AppendLine(Type.ToString());
        output.AppendLine($"current: {Current}");
        output.AppendLine($"previous: {Previous}");

        return output.ToString();
    }
}