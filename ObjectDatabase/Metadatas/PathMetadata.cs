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

    public static bool TryParse(string[] contents, out PathMetadata? pathMetadata)
    {
        pathMetadata = null;

        try
        {
            string path = contents[0];
            long lastWriteTime = long.Parse(contents[1]);
            Hash cachedPointer = Hash.Parse(contents[2]);

            Hash? metadataPointer = Database.ObjectExists(cachedPointer)
                ? cachedPointer
                : null;

            pathMetadata = new(path, lastWriteTime, metadataPointer);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public override string ToString() =>
        $"{Path}\n{LastWriteTime}\n{CachedPointer}";
}