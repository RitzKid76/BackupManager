using Backup.ObjectDatabase.Hashing;
using Backup.ObjectDatabase.Metadatas;
using Backup.ObjectDatabase.ObjectTypes;

namespace Backup.ObjectDatabase;

public class ObjectReference
{
    public string FullName { get; private set; }

    public string Name { get; private set; }
    public ObjectFormat Format { get; private set; }
    public Hash Pointer { get; private set; }

    public ObjectMetadata? Metadata { get; private set; }

    private ObjectReference(string name, ObjectFormat format, Hash pointer)
    {
        FullName = name;

        Name = name;
        Format = format;
        Pointer = pointer;
    }



    public static ObjectReference CreateBlob(string name, string pointerString) =>
        CreateBlob(name, Hash.Parse(pointerString));

    public static ObjectReference CreateBlob(string name, Hash pointer)
    {
        ObjectMetadata? metadata = Database.ReadObjectMetadata(pointer.ToString());
        return new(name, ObjectFormat.BLOB, pointer)
        {
            Metadata = metadata
        };
    }

    public static ObjectReference CreateTree(string name, Hash pointer) =>
        new(name, ObjectFormat.TREE, pointer);



    public ObjectReference? FindReferenceByPath(string path)
    {
        if (!path.StartsWith(FullName))
            return null;

        if (FullName == path)
            return this;

        Tree tree = Database.ReadTree(this);

        foreach (ObjectReference reference in tree.References)
            if (reference.FindReferenceByPath(path) is ObjectReference output)
                return output;

        return null;
    }



    public static bool TryParse(string contents, out ObjectReference? reference)
    {
        reference = null;

        if (contents.Length < 44)
            return false;

        char formatChar = contents[0];
        contents = contents[2..];

        string hashString = contents[0..40];
        string name = contents[41..];

        ObjectFormat? format = Extensions_ObjectFormat.Parse(formatChar);
        if (format is null)
            return false;

        Hash pointer = Hash.Parse(hashString);

        reference = new(name, format.Value, pointer)
        {
            Metadata = Database.ReadObjectMetadata(hashString)
        };

        return true;
    }

    public void MarkCompressed()
    {
        Metadata ??= new();

        Metadata = Metadata with
        {
            Compressed = true
        };
    }

    public bool IsCompressed() =>
        Metadata?.Compressed ?? false;

    public void SetFullName(string fullName) =>
        FullName = $"{fullName}\\{Name}";

    public override string ToString() =>
        $"{Format.GetFormatChar()} {Pointer} {Name}";
}