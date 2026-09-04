using Backup.ObjectDatabase.Hashing;
using Backup.ObjectDatabase.Metadatas;
using Backup.ObjectDatabase.ObjectTypes;

namespace Backup.ObjectDatabase;

public class ObjectReference
{
    public string Name { get; private set; }
    public ObjectFormat Format { get; private set; }
    public Hash Pointer { get; private set; }

    public ObjectMetadata? Metadata { get; private set; }

    public ObjectReference(string name, ObjectFormat format, Hash pointer)
    {
        Name = name;
        Format = format;
        Pointer = pointer;
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
            Metadata = Database.ReadObjectMetadata(pointer)
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

    public void PrependPath(string path) =>
        Name = $"{path}\\{Name}";

    public override string ToString() =>
        $"{Format.GetFormatChar()} {Pointer} {Name}";
}