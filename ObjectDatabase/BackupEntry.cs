using System.Text;

namespace Backup.ObjectDatabase;

public record BackupEntry(string Name, long CreationTime, List<ObjectReference> References)
{
    public BackupEntry(string name) :
        this(name, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), [])
    { }

    public void AddReference(ObjectReference reference) =>
        References.Add(reference);

    public static BackupEntry Parse(string name, string[] contents)
    {
        List<ObjectReference> references = [];

        string creationTimeString = contents[0];
        long creationTime = long.Parse(creationTimeString);

        foreach (string line in contents[1..])
            references.Add(ObjectReference.Parse(line));

        return new(name, creationTime, references);
    }

    public override string ToString()
    {
        StringBuilder output = new();

        output.AppendLine(CreationTime.ToString());

        foreach (ObjectReference reference in References)
            output.AppendLine(reference.ToString());

        return output.ToString();
    }
}