namespace Backup.ObjectDatabase;

public record BackupEntry(string Name, List<ObjectReference> References)
{
    public static BackupEntry Parse(string path)
    {
        string name = Util.ExtractNameFromPath(path);

        List<ObjectReference> references = [];

        string[] contents = File.ReadAllLines(path);
        foreach (string line in contents)
            references.Add(ObjectReference.Parse(line));

        return new(name, references);
    }
}