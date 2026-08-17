using Backup.Components;
using Backup.Configs;
using Backup.Extensions;
using Backup.ObjectDatabase.Hashing;
using Backup.ObjectDatabase.ObjectTypes;

namespace Backup.ObjectDatabase;

public static class Database
{
    public static ObjectReference? WriteFile(FileInfo file, bool withPrefix)
    {
        Hash? hash = Hash.Create(file);
        if (hash is null)
            return null;

        string name = withPrefix
            ? file.FullName
            : file.Name;

        ObjectReference output = new(name, ObjectFormat.BLOB, hash);

        (string databaseFolder, string databasePath) = GetDatabaseAddress(hash);
        Logger.Info($"writing: {file.FullName}");

        if (File.Exists(databasePath))
            return output;

        Directory.CreateDirectory(databaseFolder);
        file.CopyTo(databasePath);

        return output;
    }

    public static ObjectReference WriteTree(Tree tree)
    {
        string data = tree.ToString();
        Hash hash = Hash.Create(data);

        (string databaseFolder, string databasePath) = GetDatabaseAddress(hash);

        Directory.CreateDirectory(databaseFolder);

        FileInfo file = new(databasePath);
        using (StreamWriter stream = file.CreateText())
            stream.Write(data);

        return new(tree.Name, ObjectFormat.TREE, hash);
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

        (_, string databasePath) = GetDatabaseAddress(reference.Pointer);
        Logger.Info($"restoring: {path}");

        FileInfo file = new(databasePath);

        Directory.CreateDirectory(path.ExtractPathDirectory());
        file.CopyTo(path, true);
    }

    public static Tree ReadTree(ObjectReference reference)
    {
        if (reference.Format != ObjectFormat.TREE)
            throw new ArgumentException($"Expected {typeof(Tree)} but found {reference.Format}");

        (_, string databasePath) = GetDatabaseAddress(reference.Pointer);

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
        Logger.Info($"deleting: {hash}");

        (string folder, string path) = GetDatabaseAddress(hash);
        File.Delete(path);

        if (!Directory.EnumerateFiles(folder).Any())
            Directory.Delete(folder);
    }

    public static void DeleteBackup(BackupEntry backup)
    {
        string path = $"{Config.BackupFolder}\\{backup.Name}";
        File.Delete(path);

        CleanupHandler.Run();
    }

    private static (string, string) GetDatabaseAddress(Hash hash) =>
        GetDatabaseAddress(hash.ToString());

    private static (string, string) GetDatabaseAddress(string hashString)
    {
        string folder = $"{Config.DatabaseFolder}\\{hashString[0..2]}";
        string file = hashString[2..];

        return (folder, $"{folder}\\{file}");
    }
}