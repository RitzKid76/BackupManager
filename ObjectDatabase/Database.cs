using Backup.Configs;
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
        Console.WriteLine($"Writing: {file.FullName}");

        if (File.Exists(databasePath))
            return output;

        Directory.CreateDirectory(databaseFolder);
        file.CopyTo(databasePath);

        return output;
    }

    public static ObjectReference WriteTree(Tree tree)
    {
        string data = tree.GetData();
        Hash hash = Hash.Create(data);

        (string databaseFolder, string databasePath) = GetDatabaseAddress(hash);

        Directory.CreateDirectory(databaseFolder);

        FileInfo file = new(databasePath);
        using StreamWriter stream = file.CreateText();

        stream.Write(data);

        return new(tree.Name, ObjectFormat.TREE, hash);
    }

    public static void WriteBackup(Tree tree, string backupName)
    {
        string backupPath = $"{Config.BackupFolder}\\{backupName}";

        FileInfo backupFile = new(backupPath);
        using StreamWriter stream = backupFile.CreateText();

        stream.Write(tree.GetData());
    }



    public static void ReadFile(ObjectReference reference)
    {
        if (reference.Format != ObjectFormat.BLOB)
            throw new ArgumentException($"Expected BLOB but found {reference.Format}");

        string path = reference.Name;

        (string databaseFolder, string databasePath) = GetDatabaseAddress(reference.Pointer);
        Console.WriteLine($"Reading: {path}");

        FileInfo file = new(databasePath);

        Directory.CreateDirectory(databaseFolder);
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

    public static List<ObjectReference> ReadBackup(string backupName)
    {
        string path = $"{Config.BackupFolder}\\{backupName}";
        string[] contents = File.ReadAllLines(path);

        List<ObjectReference> output = [];
        foreach (string line in contents)
            output.Add(ObjectReference.Parse(line));

        return output;
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