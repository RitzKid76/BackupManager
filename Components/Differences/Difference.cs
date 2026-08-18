using System.Text;
using Backup.ObjectDatabase;
using Backup.ObjectDatabase.Hashing;

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

    public static Difference Parse(string contents)
    {
        char typeChar = contents[0];
        DifferenceType type = Extensions_DifferenceType.Parse(typeChar);

        contents = contents[2..];

        string? currentName = null;
        string? previousName = null;

        string? currentPointerString = null;
        string? previousPointerString = null;

        if (type == DifferenceType.RENAME)
        {
            previousPointerString = currentPointerString = contents[..40];

            string[] names = contents[40..].Split(" > ");
            previousName = names[0];
            currentName = names[1];
        }
        else if (type == DifferenceType.CHANGE)
        {
            previousPointerString = contents[..40];
            currentPointerString = contents[41..81];

            previousName = currentName = contents[82..];
        }
        else if (type == DifferenceType.ADDITION)
        {
            currentPointerString = contents[..40];
            currentName = contents[41..];
        }
        else if (type == DifferenceType.REMOVAL)
        {
            previousPointerString = contents[..40];
            previousName = contents[41..];
        }

        ObjectReference? current = null;
        if (currentName is not null && currentPointerString is not null)
            current = new(currentName, ObjectDatabase.ObjectTypes.ObjectFormat.BLOB, Hash.Parse(currentPointerString));

        ObjectReference? previous = null;
        if (previousName is not null && previousPointerString is not null)
            previous = new(previousName, ObjectDatabase.ObjectTypes.ObjectFormat.BLOB, Hash.Parse(previousPointerString));

        return new(type, previous, current);
    }

    public string DiffString()
    {
        StringBuilder output = new();

        output.Append($"{Type.GetTypeChar()} ");

        if (Type == DifferenceType.RENAME)
            output.Append($"{Previous!.Name} > {Current!.Name}");

        else if (
            Type == DifferenceType.CHANGE ||
            Type == DifferenceType.ADDITION
        ) output.Append(Current!.Name);

        else if (Type == DifferenceType.REMOVAL)
            output.Append(Previous!.Name);

        return output.ToString();
    }

    public void Apply()
    {
        switch (Type)
        {
            case DifferenceType.ADDITION:
            case DifferenceType.CHANGE:
                Database.RestoreFile(Current!);
                break;
            case DifferenceType.REMOVAL:
                File.Delete(Previous!.Name);
                break;
            case DifferenceType.RENAME:
                File.Move(Previous!.Name, Current!.Name);
                break;
        }
    }

    public override string ToString()
    {
        StringBuilder output = new();

        output.Append($"{Type.GetTypeChar()} ");

        if (Type == DifferenceType.RENAME)
            output.Append($"{Current!.Pointer} {Previous!.Name} > {Current.Name}");

        else if (Type == DifferenceType.CHANGE)
            output.Append($"{Previous!.Pointer} {Current!.Pointer} {Current.Name}");

        else if (Type == DifferenceType.ADDITION)
            output.Append($"{Current!.Pointer} {Current.Name}");

        else if (Type == DifferenceType.REMOVAL)
            output.Append($"{Previous!.Pointer} {Previous.Name}");

        return output.ToString();
    }
}