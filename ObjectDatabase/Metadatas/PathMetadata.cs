namespace Backup.ObjectDatabase.Metadatas;

public record PathMetadata(string Path, long ModificationTime)
{
    public static PathMetadata Parse(string[] contents)
    {
        string path = contents[0];
        long modificationTime = long.Parse(contents[1]);

        return new(path, modificationTime);
    }

    public override string ToString() =>
        $"{Path}\n{ModificationTime}";
}