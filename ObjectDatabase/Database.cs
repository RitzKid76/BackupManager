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

    public static void Delete(Hash hash)
    {
        string hashString = hash.ToString();

        Logger.Info($"deleting: {hashString}");

        (string folder, string path) = GetDatabaseAddress(hashString);
        File.Delete(path);

        (string metaFolder, string metaPath) = GetMetaDatabaseAddress(hashString);
        if (File.Exists(metaPath))
        {
            Logger.Info($"deleting: {hashString} object metadata");
            File.Delete(metaPath);
        }

        if (Directory.EnumerateFiles(folder).Any())
            return;

        Directory.Delete(folder);

        if (Directory.Exists(metaFolder))
            Directory.Delete(metaFolder);
    }

    public static List<Hash> GetAllPointers()
    {
        List<Hash> output = [];

        string[] buckets = Directory.GetDirectories(Config.DatabaseFolder);
        foreach (string bucket in buckets)
        {
            string bucketName = bucket.ExtractPathName();

            if (bucketName == METADATA_FOLDER)
                continue;

            string[] pointers = Directory.GetFiles(bucket);
            foreach (string pointer in pointers)
            {
                string pointerName = pointer.ExtractPathName();
                string hashString = $"{bucketName}{pointerName}";

                output.Add(Hash.Parse(hashString));
            }
        }

        return output;
    }

    public static List<PathMetadata> GetAllPathMetas()
    {
        List<string> metaPaths = [];
        List<PathMetadata> output = [];


        string folderPath = $"{Config.DatabaseFolder}/{METADATA_FOLDER}";
        if (!Directory.Exists(folderPath))
            return [];

        string[] buckets = Directory.GetDirectories(folderPath);
        foreach (string bucket in buckets)
            metaPaths.AddRange(Directory.GetFiles(bucket));

        foreach (string path in metaPaths)
        {
            string[] contents = File.ReadAllLines(path);

            if (PathMetadata.TryParse(contents, out PathMetadata? metadata))
                output.Add(metadata!);
        }

        return output;
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
            return null;

        if (!noChanges)
        {
            newMetaData.CachePointer(hash);
            WritePathMetadata(newMetaData);
        }

        ObjectReference output = new(name, ObjectFormat.BLOB, hash);

        if (noChanges)
        {
            Logger.Info($"*skipping: {hash} {file.FullName}");
            return output;
        }

        return WriteFileBlob(file, output);
    }

    private static ObjectReference? WriteFileSkipOptimization(FileInfo file, string name)
    {
        Hash? hash = Hash.Create(file);
        if (hash is null)
            return null;

        ObjectReference output = new(name, ObjectFormat.BLOB, hash);

        return WriteFileBlob(file, output);
    }

    private static ObjectReference WriteFileBlob(FileInfo file, ObjectReference reference)
    {
        (string databaseFolder, string databasePath) = GetDatabaseAddress(reference.Pointer.ToString());

        if (File.Exists(databasePath))
        {
            Logger.Info($" skipping: {reference.Pointer} {file.FullName}");
            return reference;
        }

        Logger.Info($"  writing: {reference.Pointer} {file.FullName}");
        Directory.CreateDirectory(databaseFolder);

        bool isCompressed = GZIP.Write(file, databasePath);
        if (isCompressed)
            reference.MarkCompressed();

        if (reference.Metadata is not null)
            WriteObjectMetadata(reference.Pointer, reference.Metadata);

        return reference;
    }

    public static void RestoreFile(ObjectReference reference)
    {
        if (reference.Format != ObjectFormat.BLOB)
            throw new ArgumentException($"Expected BLOB but found {reference.Format}");

        string path = reference.Name;

        (_, string databasePath) = GetDatabaseAddress(reference.Pointer.ToString());
        Logger.Info($"restoring: {path}");

        FileInfo file = new(databasePath);

        Directory.CreateDirectory(path.ExtractPathDirectory());

        if (reference.IsCompressed())
            GZIP.Read(file, path);
        else
            file.CopyTo(path, true);
    }



    public static ObjectReference WriteTree(Tree tree)
    {
        string data = tree.ToString();
        Hash hash = Hash.Create(data);

        ObjectReference output = new(tree.Name, ObjectFormat.TREE, hash);

        (string databaseFolder, string databasePath) = GetDatabaseAddress(hash.ToString());

        if (File.Exists(databasePath))
            return output;

        Directory.CreateDirectory(databaseFolder);

        FileInfo file = new(databasePath);
        using (StreamWriter stream = file.CreateText())
            stream.Write(data);

        return output;
    }

    public static Tree ReadTree(ObjectReference reference)
    {
        if (reference.Format != ObjectFormat.TREE)
            throw new ArgumentException($"Expected {typeof(Tree)} but found {reference.Format}");

        (_, string databasePath) = GetDatabaseAddress(reference.Pointer.ToString());

        string[] contents = File.ReadAllLines(databasePath);
        return Tree.Parse(reference.FullName, reference.Name, contents);
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

        return output;
    }

    public static void DeletePathMetadata(PathMetadata metadata)
    {
        Hash hash = Hash.Create(metadata.Path);
        (string databaseFolder, string databasePath) = GetMetaDatabaseAddress(hash.ToString());

        File.Delete(databasePath);

        if (Directory.EnumerateFiles(databaseFolder).Any())
            return;

        Logger.Info($"deleting: {metadata.CachedPointer} path metadata");
        Directory.Delete(databaseFolder);
    }

    public static void WriteObjectMetadata(Hash pointer, ObjectMetadata metadata)
    {
        (string databaseFolder, string databasePath) = GetMetaDatabaseAddress(pointer.ToString());
        Directory.CreateDirectory(databaseFolder);

        FileInfo metadataFile = new(databasePath);
        using (StreamWriter stream = metadataFile.CreateText())
            stream.Write(metadata.ToString());
    }

    public static ObjectMetadata? ReadObjectMetadata(Hash pointer)
    {
        (_, string databasePath) = GetMetaDatabaseAddress(pointer.ToString());

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