using System.Text;
using Backup.Components;
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

            output.Differences.Add(Difference.Parse(line));
        }

        return output;
    }

    public void RegenerateDifference()
    {
        Logger.Info("regenerating diff...");
        if (BackupDatabase.Count() == 0)
            return;

        Differences.Clear();
        int ourIndex = BackupDatabase.IndexOf(Name);
        if (!BackupDatabase.TryGetBackup(ourIndex + 1, out BackupEntry? previous))
            return;

        List<Difference> differences = DifferenceGenerator.FromBackup(previous!, this);
        Differences.AddRange(differences);
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

        foreach (ObjectReference reference in References.OrderBy(r => r.FullName))
            output.AppendLine(reference.ToString());

        foreach (Difference difference in Differences)
            output.AppendLine(difference.ToString());

        return output.ToString();
    }
}