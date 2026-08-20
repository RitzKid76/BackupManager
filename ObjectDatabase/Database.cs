using Backup.Components;
using Backup.Configs;
using Backup.Extensions;
using Backup.ObjectDatabase.Hashing;
using Backup.ObjectDatabase.ObjectTypes;

namespace Backup.ObjectDatabase;

public static class Database
{
    private const string COMPRESSED_META = "_c";

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
            Logger.Info($"skipping: {file.FullName}");
            return output;
        }

        Logger.Info($"writing: {file.FullName}");
        Directory.CreateDirectory(databaseFolder);

        bool isCompressed = GZIP.Write(file, databasePath);
        if (isCompressed)
        {
            MarkCompressed(hashString);
            output.MarkCompressed();
        }

        return output;
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

    public static void WriteBackup(BackupEntry backup)
    {
        string backupPath = $"{Config.BackupFolder}\\{backup.Name}";

        FileInfo backupFile = new(backupPath);
        using (StreamWriter stream = backupFile.CreateText())
            stream.Write(backup.ToString());

        CleanupHandler.Run();
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

    public static Tree ReadTree(ObjectReference reference)
    {
        if (reference.Format != ObjectFormat.TREE)
            throw new ArgumentException($"Expected {typeof(Tree)} but found {reference.Format}");

        (_, string databasePath) = GetDatabaseAddress(reference.Pointer.ToString());

        string[] contents = File.ReadAllLines(databasePath);
        return Tree.Parse(reference.Name, contents);
    }

    public static BackupEntry ReadBackup(string path)
    {
        string backupName = path.ExtractPathName();

        string[] contents = File.ReadAllLines(path);
        return BackupEntry.Parse(backupName, contents);
    }



    public static List<Hash> GetAllPointers()
    {
        List<Hash> output = [];

        string[] buckets = Directory.GetDirectories(Config.DatabaseFolder);
        foreach (string bucket in buckets)
        {
            string bucketName = bucket.ExtractPathName();

            if (bucketName == COMPRESSED_META)
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

    public static void Delete(Hash hash)
    {
        string hashString = hash.ToString();

        Logger.Info($"deleting: {hashString}");

        (string folder, string path) = GetDatabaseAddress(hashString);
        File.Delete(path);

        (string compressedFolder, string compressedPath) = GetCompressedDatabaseAddress(hashString);
        if (File.Exists(compressedPath))
            File.Delete(compressedPath);

        if (Directory.EnumerateFiles(folder).Any())
            return;

        Directory.Delete(folder);

        if (Directory.Exists(compressedFolder))
            Directory.Delete(compressedFolder);
    }

    public static void DeleteBackup(BackupEntry backup)
    {
        string path = $"{Config.BackupFolder}\\{backup.Name}";
        File.Delete(path);

        CleanupHandler.Run();
    }

    public static bool IsBlobCompressed(string hashString)
    {
        (_, string databasePath) = GetCompressedDatabaseAddress(hashString);
        return File.Exists(databasePath);
    }


    private static (string, string) GetDatabaseAddress(string hashString)
    {
        string folder = $"{Config.DatabaseFolder}\\{hashString[0..2]}";
        string file = hashString[2..];

        return (folder, $"{folder}\\{file}");
    }

    private static (string, string) GetCompressedDatabaseAddress(string hashString)
    {
        string folder = $"{Config.DatabaseFolder}\\{COMPRESSED_META}\\{hashString[0..2]}";
        string file = hashString[2..];

        return (folder, $"{folder}\\{file}");
    }

    private static void MarkCompressed(string hashString)
    {
        (string folder, string path) = GetCompressedDatabaseAddress(hashString);

        Directory.CreateDirectory(folder);
        File.Create(path).Dispose();
    }
}