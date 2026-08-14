using Backup.ObjectDatabase.Hashing;
using Backup.ObjectDatabase.ObjectTypes;

namespace Backup.ObjectDatabase;

public static class Database
{
    private const string BACKUP_FOLDER = "T:\\database\\";

    public static ObjectReference? WriteFile(FileInfo file)
    {
        Hash? hash = Hash.Create(file);
        if (hash is null)
            return null;

        ObjectReference output = new(file.Name, DatabaseObjectType.BLOB, hash);

        (string databaseFolder, string databasePath) = GetDatabaseFolderAndPath(hash);
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

        (string databaseFolder, string databasePath) = GetDatabaseFolderAndPath(hash);

        Directory.CreateDirectory(databaseFolder);
        
        FileInfo file = new(databasePath);
        using StreamWriter stream = file.CreateText();

        stream.Write(data);

        return new(tree.Name, DatabaseObjectType.TREE, hash);
    }

    private static (string, string) GetDatabaseFolderAndPath(Hash hash)
    {
        string hashString = hash.ToString();

        string folder = $"{BACKUP_FOLDER}\\{hashString[0..2]}";
        string file = hashString[2..];

        return (folder, $"{folder}\\{file}");
    }
}