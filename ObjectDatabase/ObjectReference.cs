using Backup.ObjectDatabase.Hashing;
using Backup.ObjectDatabase.ObjectTypes;

namespace Backup.ObjectDatabase;

public class ObjectReference
{
    public string Name { get; private set; }
    public ObjectFormat Format { get; private set; }
    public Hash Pointer { get; private set; }

    private bool compressed;

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

        char compressionChar = contents[1];
        bool compressed = compressionChar == 'C';

        int offset = compressed
            ? 3
            : 2;

        contents = contents[offset..];

        string hashString = contents[0..40];
        string name = contents[41..];

        ObjectFormat? format = Extensions_ObjectFormat.Parse(formatChar);
        if (format is null)
            return false;

        Hash pointer = Hash.Parse(hashString);

        output = new(name, format.Value, pointer);
        output.MarkCompressed(compressed);

        return true;
    }

    public void MarkCompressed(bool compressed) =>
        this.compressed = compressed;

    public bool IsCompressed() =>
        compressed;

    private char? CompressionChar() => compressed
        ? 'C'
        : null;

    public void PrependPath(string path) =>
        Name = $"{path}\\{Name}";

    public override string ToString() =>
        $"{Format.GetFormatChar()}{CompressionChar()} {Pointer} {Name}";
}