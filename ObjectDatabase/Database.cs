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

        (string compressedFolder, string compressedPath) = GetMetaDatabaseAddress(hashString);
        if (File.Exists(compressedPath))
            File.Delete(compressedPath);

        if (Directory.EnumerateFiles(folder).Any())
            return;

        Directory.Delete(folder);

        if (Directory.Exists(compressedFolder))
            Directory.Delete(compressedFolder);
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



    public static ObjectReference? WriteFile(FileInfo file, bool withPrefix)
    {
        Hash? hash = Hash.Create(file);
        if (hash is null)
            return null;

        string name = withPrefix
            ? file.FullName
            : file.Name;

        ObjectReference output = new(name, ObjectFormat.BLOB, hash);

        string hashString = hash.ToString();

        (string databaseFolder, string databasePath) = GetDatabaseAddress(hashString);

        if (File.Exists(databasePath))
        {
            Logger.Info($"skipping: {hash} {file.FullName}");
            return output;
        }

        Logger.Info($" writing: {hash} {file.FullName}");
        Directory.CreateDirectory(databaseFolder);

        bool isCompressed = GZIP.Write(file, databasePath);
        if (isCompressed)
            output.MarkCompressed();

        if (output.Metadata is not null)
            WriteObjectMetadata(output.Pointer, output.Metadata);

        return output;
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
        return Tree.Parse(reference.Name, contents);
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

    public static PathMetadata ReadPathMetadata(string path)
    {
        Hash hash = Hash.Create(path);
        (_, string databasePath) = GetMetaDatabaseAddress(hash.ToString());

        string[] contents = File.ReadAllLines(databasePath);
        return PathMetadata.Parse(contents);
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