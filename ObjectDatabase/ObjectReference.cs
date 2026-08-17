using Backup.ObjectDatabase.Hashing;
using Backup.ObjectDatabase.ObjectTypes;

namespace Backup.ObjectDatabase;

public class ObjectReference
{
    public string Name { get; private set; }
    public ObjectFormat Format { get; private set; }
    public Hash Pointer { get; private set; }

    public ObjectReference(string name, ObjectFormat format, Hash pointer)
    {
        Name = name;
        Format = format;
        Pointer = pointer;
    }

    public static bool TryParse(string contents, out ObjectReference? output)
    {
        output = null;

        if (contents.Length < 44)
            return false;

        char formatChar = contents[0];
        string hashString = contents[2..42];
        string name = contents[43..];

        ObjectFormat? format = Extensions_ObjectFormat.Parse(formatChar);
        if (format is null)
            return false;

        Hash pointer = Hash.Parse(hashString);

        output = new(name, format.Value, pointer);
        return true;
    }

    public void PrependPath(string path) =>
        Name = $"{path}\\{Name}";

    public override string ToString() =>
        $"{Format.GetFormatChar()} {Pointer} {Name}";
}