using System.Text;
using Backup.Components.Differences;

namespace Backup.ObjectDatabase;

public class BackupEntry
{
    public string Name { get; private set; }

    public readonly long CreationTime;
    public readonly List<ObjectReference> References = [];
    public readonly List<Difference> Differences = [];

    public BackupEntry(string name) :
        this(name, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
    { }

    private BackupEntry(string name, long creationTime)
    {
        Name = name;
        CreationTime = creationTime;
    }

    public void AddReference(ObjectReference reference) =>
        References.Add(reference);

    public void AddDifference(Difference difference) =>
        Differences.Add(difference);

    public static BackupEntry Parse(string name, string[] contents)
    {
        string creationTimeString = contents[0];
        long creationTime = long.Parse(creationTimeString);

        BackupEntry output = new(name, creationTime);

        foreach (string line in contents[1..])
        {
            if (ObjectReference.TryParse(line, out ObjectReference? reference))
            {
                output.AddReference(reference!);
                continue;
            }

            output.AddDifference(Difference.Parse(line));
        }

        return output;
    }

    public string GetDiffString()
    {
        StringBuilder output = new();

        foreach (Difference difference in Differences)
            output.AppendLine(difference.DiffString());

        return output.ToString();
    }

    public override string ToString()
    {
        StringBuilder output = new();

        output.AppendLine(CreationTime.ToString());

        foreach (ObjectReference reference in References)
            output.AppendLine(reference.ToString());

        foreach (Difference difference in Differences)
            output.AppendLine(difference.ToString());

        return output.ToString();
    }
}