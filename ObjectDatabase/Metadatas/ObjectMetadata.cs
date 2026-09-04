namespace Backup.ObjectDatabase.Metadatas;

public record ObjectMetadata(bool Compressed)
{
    public ObjectMetadata() :
        this(false)
    { }

    public static ObjectMetadata Parse(string[] contents) =>
        new(contents[0] == "C");

    public override string ToString() => Compressed
            ? "C"
            : "U";
}