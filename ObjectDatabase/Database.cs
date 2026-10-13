using System.Collections.Concurrent;
using Backup.Components;
using Backup.Configs;
using Backup.Extensions;
using Backup.ObjectDatabase.Hashing;
using Backup.ObjectDatabase.Metadatas;
using Backup.ObjectDatabase.ObjectTypes;

namespace Backup.ObjectDatabase;

public static class Database
{
    private const string METADATA_FOLDER = "_m";

    private static readonly ConcurrentDictionary<string, object> objectWriteLocks = new();

    public static void Delete(Hash hash)
    {
        string hashString = hash.ToString();

        Logger.InfoColorize($"deleting: {hashString}", ConsoleColor.Red);

        (string folder, string path) = GetDatabaseAddress(hashString);
        File.Delete(path);

        (string metaFolder, string metaPath) = GetMetaDatabaseAddress(hashString);
        if (File.Exists(metaPath))
        {
            Logger.InfoColorize($"deleting: {hashString} object metadata", ConsoleColor.DarkRed);
            File.Delete(metaPath);
        }

        if (Directory.EnumerateFiles(folder).Any())
            return;

        Directory.Delete(folder);

        if (Directory.Exists(metaFolder))
            Directory.Delete(metaFolder);
    }

    public static IEnumerable<Hash> GetAllPointers()
    {
        foreach (string bucket in Directory.EnumerateDirectories(Config.DatabaseFolder))
        {
            string bucketName = bucket.ExtractPathName();
            if (bucketName == METADATA_FOLDER)
                continue;

            foreach (string pointer in Directory.EnumerateFiles(bucket))
            {
                string pointerName = pointer.ExtractPathName();
                string hashString = $"{bucketName}{pointerName}";

                yield return Hash.Parse(hashString);
            }
        }
    }

    public static IEnumerable<PathMetadata> GetAllPathMetas()
    {
        string folderPath = $"{Config.DatabaseFolder}/{METADATA_FOLDER}";
        if (!Directory.Exists(folderPath))
            yield break;

        foreach (string bucket in Directory.EnumerateDirectories(folderPath))
        {
            foreach (string path in Directory.EnumerateFiles(bucket))
            {
                string[] contents = File.ReadAllLines(path);
                if (PathMetadata.TryParse(contents, out PathMetadata? metadata))
                    yield return metadata!;
            }
        }
    }

    public static bool ObjectExists(Hash pointer)
    {
        (_, string databasePath) = GetDatabaseAddress(pointer.ToString());
        return File.Exists(databasePath);
    }



    public static ObjectReference? WriteFile(FileInfo file, bool withPrefix)
    {
        string name = withPrefix
            ? file.FullName
            : file.Name;

        if (file.Length < Config.WriteTimeOptimizationThreshold)
            return WriteFileSkipOptimization(file, name);

        PathMetadata? metadata = ReadPathMetadata(file.FullName);
        PathMetadata newMetaData = PathMetadata.Create(file);

        bool noChanges = metadata is not null && newMetaData.LastWriteTime <= metadata.LastWriteTime;

        Hash? hash = noChanges
            ? metadata!.CachedPointer
            : Hash.Create(file);

        if (hash is null)
        {
            Logger.Colorize($"X  failed: {file.FullName}", ConsoleColor.DarkRed);
            return null;
        }

        if (!noChanges)
        {
            newMetaData.CachePointer(hash);
            WritePathMetadata(newMetaData);
        }

        ObjectReference output = ObjectReference.CreateBlob(name, hash);

        if (noChanges)
        {
            Logger.InfoColorize($"*skipping: {hash} {file.FullName}", ConsoleColor.DarkGray);
            return output;
        }

        return WriteFileBlob(file, output);
    }

    private static ObjectReference? WriteFileSkipOptimization(FileInfo file, string name)
    {
        Hash? hash = Hash.Create(file);
        if (hash is null)
        {
            Logger.Colorize($"X  failed: {file.FullName}", ConsoleColor.DarkRed);
            return null;
        }

        ObjectReference output = ObjectReference.CreateBlob(name, hash);

        return WriteFileBlob(file, output);
    }

    private static ObjectReference WriteFileBlob(FileInfo file, ObjectReference reference)
    {
        (string databaseFolder, string databasePath) = GetDatabaseAddress(reference.Pointer.ToString());

        if (File.Exists(databasePath))
        {
            Logger.InfoColorize($" skipping: {reference.Pointer} {file.FullName}", ConsoleColor.DarkGray);
            return reference;
        }

        object lockObj = objectWriteLocks.GetOrAdd(reference.Pointer.ToString(), _ => new object());
        lock (lockObj)
        {
            if (File.Exists(databasePath))
            {
                Logger.InfoColorize($" skipping: {reference.Pointer} {file.FullName}", ConsoleColor.DarkGray);
                return reference;
            }

            Logger.InfoColorize($"  writing: {reference.Pointer} {file.FullName}", ConsoleColor.Green);
            Directory.CreateDirectory(databaseFolder);

            bool isCompressed = GZIP.Write(file, databasePath);
            if (isCompressed)
                reference.MarkCompressed();

            if (reference.Metadata is not null)
                WriteObjectMetadata(reference.Pointer, reference.Metadata);

            return reference;
        }
    }

    public static string[] ReadFile(ObjectReference reference)
    {
        if (reference.Format != ObjectFormat.BLOB)
            throw new ArgumentException($"Expected BLOB but found {reference.Format}");

        (_, string databasePath) = GetDatabaseAddress(reference.Pointer.ToString());

        return reference.IsCompressed()
            ? GZIP.Read(databasePath)
            : File.ReadAllLines(databasePath);
    }

    public static void RestoreFile(ObjectReference reference)
    {
        if (reference.Format != ObjectFormat.BLOB)
            throw new ArgumentException($"Expected BLOB but found {reference.Format}");

        string path = reference.FullName;

        (_, string databasePath) = GetDatabaseAddress(reference.Pointer.ToString());
        Logger.InfoColorize($"restoring: {path}", ConsoleColor.Magenta);

        FileInfo file = new(databasePath);

        Directory.CreateDirectory(path.ExtractPathDirectory());

        if (reference.IsCompressed())
            GZIP.CopyTo(file, path);
        else
            file.CopyTo(path, true);
    }



    public static ObjectReference WriteTree(Tree tree)
    {
        string data = tree.ToString();
        Hash hash = Hash.Create(data);

        ObjectReference output = ObjectReference.CreateTree(tree.Name, hash);

        (string databaseFolder, string databasePath) = GetDatabaseAddress(hash.ToString());

        if (File.Exists(databasePath))
            return output;

        Directory.CreateDirectory(databaseFolder);

        FileInfo file = new(databasePath);
        using (StreamWriter stream = file.CreateText())
            stream.Write(data);

        return output;
    }

    public static bool TryReadTree(ObjectReference reference, out Tree? output)
    {
        output = null;

        if (reference.Format != ObjectFormat.TREE)
            return false;

        (_, string databasePath) = GetDatabaseAddress(reference.Pointer.ToString());

        string[] contents = File.ReadAllLines(databasePath);
        return Tree.TryParse(reference.FullName, reference.Name, contents, out output);
    }

    public static bool TryReadUnknownTree(string pointer, out Tree? output)
    {
        (_, string databasePath) = GetDatabaseAddress(pointer);

        string[] contents = File.ReadAllLines(databasePath);
        return Tree.TryParse(string.Empty, string.Empty, contents, out output);
    }



    public static void WriteBackup(BackupEntry backup)
    {
        string backupPath = $"{Config.BackupFolder}\\{backup.Name}";

        FileInfo backupFile = new(backupPath);
        using (StreamWriter stream = backupFile.CreateText())
            stream.Write(backup.ToString());

        CleanupHandler.Run();
    }

    public static BackupEntry ReadBackup(string path)
    {
        string backupName = path.ExtractPathName();

        string[] contents = File.ReadAllLines(path);
        return BackupEntry.Parse(backupName, contents);
    }

    public static void DeleteBackup(BackupEntry backup)
    {
        string path = $"{Config.BackupFolder}\\{backup.Name}";
        File.Delete(path);

        CleanupHandler.Run();
    }



    public static void WritePathMetadata(PathMetadata metadata)
    {
        Logger.InfoColorize($"  writing: {metadata.CachedPointer} {metadata.Path} path metadata", ConsoleColor.DarkGreen);

        Hash hash = Hash.Create(metadata.Path);
        (string databaseFolder, string databasePath) = GetMetaDatabaseAddress(hash.ToString());

        Directory.CreateDirectory(databaseFolder);

        FileInfo metadataFile = new(databasePath);
        using (StreamWriter stream = metadataFile.CreateText())
            stream.Write(metadata.ToString());
    }

    public static PathMetadata? ReadPathMetadata(string path)
    {
        Hash hash = Hash.Create(path);
        (_, string databasePath) = GetMetaDatabaseAddress(hash.ToString());

        if (!File.Exists(databasePath))
            return null;

        string[] contents = File.ReadAllLines(databasePath);
        PathMetadata.TryParse(contents, out PathMetadata? output);

        if (output?.CachedPointer is null)
            return null;

        return output;
    }

    public static void DeletePathMetadata(PathMetadata metadata)
    {
        Hash hash = Hash.Create(metadata.Path);
        (string databaseFolder, string databasePath) = GetMetaDatabaseAddress(hash.ToString());

        File.Delete(databasePath);

        if (Directory.EnumerateFiles(databaseFolder).Any())
            return;

        Logger.InfoColorize($"deleting: {metadata.CachedPointer} path metadata", ConsoleColor.DarkRed);
        Directory.Delete(databaseFolder);
    }

    public static void WriteObjectMetadata(Hash pointer, ObjectMetadata metadata)
    {
        Logger.InfoColorize($"  writing: {pointer} object metadata", ConsoleColor.DarkGreen);

        (string databaseFolder, string databasePath) = GetMetaDatabaseAddress(pointer.ToString());
        Directory.CreateDirectory(databaseFolder);

        FileInfo metadataFile = new(databasePath);
        using (StreamWriter stream = metadataFile.CreateText())
            stream.Write(metadata.ToString());
    }

    public static ObjectMetadata? ReadObjectMetadata(string pointerString)
    {
        (_, string databasePath) = GetMetaDatabaseAddress(pointerString);

        if (!File.Exists(databasePath))
            return null;

        string[] contents = File.ReadAllLines(databasePath);
        return ObjectMetadata.Parse(contents);
    }



    private static (string, string) GetDatabaseAddress(string hashString)
    {
        string folder = $"{Config.DatabaseFolder}\\{hashString[0..2]}";
        string file = hashString[2..];

        return (folder, $"{folder}\\{file}");
    }

    private static (string, string) GetMetaDatabaseAddress(string hashString)
    {
        string folder = $"{Config.DatabaseFolder}\\{METADATA_FOLDER}\\{hashString[0..2]}";
        string file = hashString[2..];

        return (folder, $"{folder}\\{file}");
    }
}