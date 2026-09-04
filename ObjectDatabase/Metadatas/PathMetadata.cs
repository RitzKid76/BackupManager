using Backup.ObjectDatabase.Hashing;

namespace Backup.ObjectDatabase.Metadatas;

public class PathMetadata
{
    public string Path { get; private set; }
    public long LastWriteTime { get; private set; }
    public Hash? CachedPointer { get; private set; }

    public PathMetadata(string path, long lastWriteTime, Hash? cachedPointer = null)
    {
        Path = path;
        LastWriteTime = lastWriteTime;
        CachedPointer = cachedPointer;
    }

    public static PathMetadata Create(FileInfo file)
    {
        DateTimeOffset dto = new(file.LastWriteTimeUtc);
        long lastWriteTime = dto.ToUnixTimeMilliseconds();

        return new(file.FullName, lastWriteTime);
    }

    public void CachePointer(Hash pointer) =>
        CachedPointer = pointer;

    public static PathMetadata Parse(string[] contents)
    {
        string path = contents[0];
        long modificationTime = long.Parse(contents[1]);
        Hash cachedPointer = Hash.Parse(contents[2]);

        return new(path, modificationTime, cachedPointer);
    }

    public override string ToString() =>
        $"{Path}\n{LastWriteTime}\n{CachedPointer}";
}